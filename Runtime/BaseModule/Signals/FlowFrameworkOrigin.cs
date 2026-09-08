using System;
using System.Reflection;

namespace FlowIoC.BaseModule.Signals
{
    /// <summary>
    /// Tells the framework's own code from the game's, by the assembly a type is declared in.
    ///
    /// It decides which channel a line goes on and whether that line works out where it came from.
    /// A signal the game dispatched and a Command the game wrote are lines the reader can open;
    /// the framework registering a screen is not, and putting the two together made a reader wade
    /// through the framework's own traffic to find their own.
    ///
    /// Signals are read from their holder rather than from a flag on each one, because the holder
    /// is where the answer already is and a flag on thirty fields is thirty chances to forget one.
    /// </summary>
    public class FlowFrameworkOrigin
    {
        private const BindingFlags Fields =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public bool IsFrameworkType(Type type)
        {
            return type != null && type.Assembly == typeof(FlowFrameworkOrigin).Assembly;
        }

        /// <summary>
        /// Walks a holder once and marks every signal in it, its Incoming and Outgoing halves
        /// included. Done when the holder is bound, so a dispatch reads a bool.
        ///
        /// The same walk names each signal for where it sits: <c>PlayerSignals.Incoming.AddCurrency</c>
        /// rather than <c>AddCurrency</c>. A signal is constructed with nothing but its own field
        /// name, which the compiler supplies through <c>[CallerMemberName]</c>, and a name like
        /// <c>Launch</c> answers none of the questions a reader has when they see it dispatched -
        /// whose Launch, out of which holder, and which half of it. The walk already has the owner
        /// chain in hand, so the qualified name costs one string per signal at bind time and
        /// nothing at all per dispatch.
        /// </summary>
        public void StampSignalHolder(object holder)
        {
            if (holder == null) return;

            Stamp(holder, IsFrameworkType(holder.GetType()), holder.GetType().Name, 0);
        }

        private void Stamp(object owner, bool isFramework, string path, int depth)
        {
            // Incoming and Outgoing are one level down, and nothing a holder holds goes deeper.
            // A bound depth is what keeps a holder that happens to reference itself from hanging.
            if (owner == null || depth > 2) return;

            FieldInfo[] fields = owner.GetType().GetFields(Fields);

            for (int i = 0; i < fields.Length; i++)
            {
                object value = fields[i].GetValue(owner);
                if (value == null) continue;

                if (value is ISignalBody signal)
                {
                    signal.IsFrameworkOwned = isFramework;

                    // Composed from the field rather than appended to what the signal already
                    // carries, so a holder stamped twice is named the same both times.
                    signal.Name = path + "." + fields[i].Name;
                    continue;
                }

                if (value.GetType().IsClass && value.GetType() != typeof(string))
                    Stamp(value, isFramework, path + "." + fields[i].Name, depth + 1);
            }
        }
    }
}
