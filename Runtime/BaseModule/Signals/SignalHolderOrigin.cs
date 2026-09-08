using System;
using System.Reflection;

namespace FlowIoC.BaseModule.Signals
{
    /// <summary>
    /// Tells the framework's own signals from the game's, and marks them so a dispatch does not
    /// have to work it out again.
    ///
    /// Which one a signal is decides the channel its dispatch is logged on, and whether that log
    /// works out where it was dispatched from. A game's signal is a line the reader wrote and can
    /// open; the framework registering a screen is not.
    ///
    /// Read from the holder's assembly rather than from a flag on each signal, because the holder
    /// is where the answer already is and a flag on thirty fields is thirty chances to forget one.
    /// </summary>
    public class SignalHolderOrigin
    {
        private const BindingFlags Fields =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public bool IsFrameworkHolder(Type holderType)
        {
            return holderType != null && holderType.Assembly == typeof(SignalHolderOrigin).Assembly;
        }

        /// <summary>
        /// Walks a holder once and marks every signal in it, its Incoming and Outgoing halves
        /// included. Done when the holder is bound, so a dispatch reads a bool.
        /// </summary>
        public void Stamp(object holder)
        {
            if (holder == null) return;

            Stamp(holder, IsFrameworkHolder(holder.GetType()), 0);
        }

        private void Stamp(object owner, bool isFramework, int depth)
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
                    continue;
                }

                if (value.GetType().IsClass && value.GetType() != typeof(string))
                    Stamp(value, isFramework, depth + 1);
            }
        }
    }
}
