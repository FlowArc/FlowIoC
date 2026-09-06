using System;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.CommandGroup;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Controller
{
    public abstract class CommandBody : ICommandBody
    {
        public ICommandGroupResolver CommandGroupResolver { get; set; }
        public bool IsRetain { get; set; }
        public bool HasRetain { get; set; }

        /// <summary>
        /// The context this instance was last filled from, and the binding generation it was filled
        /// at. A pooled command asks for the same models and signals on every run and the container
        /// answers the same way, so the members are resolved again only once one of these two moves.
        /// </summary>
        internal IContext InjectedContext;

        internal int InjectionStamp = -1;

        public virtual void Retain()
        {
            IsRetain = true;
            HasRetain = true;
        }

        public virtual void Release(params object[] commandGroupData)
        {
            if (CommandGroupResolver == null)
            {
                LogDetachedCall(nameof(Release));
                return;
            }

            CommandGroupResolver.ReleaseCommand(this, commandGroupData);
        }

        public virtual void Stop()
        {
            if (CommandGroupResolver == null)
            {
                LogDetachedCall(nameof(Stop));
                return;
            }

            CommandGroupResolver.StopCommand(this);
        }

        /// <summary>
        /// Clears what the run left behind, as the command goes back to the pool. It does not
        /// touch <see cref="HasRetain"/>: a command that retained and released inside one
        /// Execute passes through here before the step is judged, and clearing the flag there
        /// would make the step look synchronous and complete it a second time.
        /// <see cref="BeginRun"/> is where the flag is cleared instead.
        /// </summary>
        public virtual void Clean()
        {
            IsRetain = false;
            CommandGroupResolver = null;
        }

        /// <summary>
        /// Readies a pooled instance for one execution. Both retain flags belong to a single run,
        /// and leaving either of them set carried the last run's answer into this one - a command
        /// that retains only on some branch was then treated as retained forever, and the sequence
        /// waited for a Release that was never coming.
        /// </summary>
        internal void BeginRun(ICommandGroupResolver commandGroupResolver)
        {
            IsRetain = false;
            HasRetain = false;
            CommandGroupResolver = commandGroupResolver;
        }

        /// <summary>
        /// The typed entry point the group resolver calls. Each arity overrides it and hands the
        /// payload to its own Execute, so a dispatch costs a virtual call rather than a metadata
        /// lookup and a reflective invoke.
        /// </summary>
        internal abstract void InvokeExecute(object[] parameters);

        /// <summary>
        /// Whether the payload holds as many values as this Execute takes. Reported rather than
        /// thrown, because the mismatch is authored in a Context and the reader needs to be told
        /// which binding to look at.
        /// </summary>
        internal bool HasArity(object[] parameters, int expected)
        {
            int provided = parameters?.Length ?? 0;
            if (provided == expected)
                return true;

            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► Execute signature mismatch!</color></b>\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Expects:</color><color=#FFEFD5> " + expected + " parameter(s)</color></b>\n" +
                "<b><color=#FF6666>► Signal carried:</color><color=#FFEFD5> " + provided + "</color></b>",
                "Execute signature mismatch on " + GetType().Name + ": it takes " + expected +
                " parameter(s) and the signal carried " + provided + ".");

            return false;
        }

        /// <summary>
        /// Reads one payload slot as <typeparamref name="T"/>. A dispatched null fills a slot whose
        /// type can hold one, which is what keeps an optional payload from shifting every value
        /// after it.
        /// </summary>
        internal bool TryFill<T>(object[] parameters, int index, out T value)
        {
            object raw = parameters[index];

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;

            if (raw == null)
            {
                Type slotType = typeof(T);
                if (!slotType.IsValueType || Nullable.GetUnderlyingType(slotType) != null)
                    return true;

                LogSlotMismatch(index, slotType.Name, "null");
                return false;
            }

            LogSlotMismatch(index, typeof(T).Name, raw.GetType().Name);
            return false;
        }

        private void LogSlotMismatch(int index, string expectedTypeName, string providedTypeName)
        {
            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► Execute parameter mismatch!</color></b>\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Parameter:</color><color=#FFEFD5> " + index + "</color></b>\n" +
                "<b><color=#FF6666>► Expected:</color><color=#FFEFD5> " + expectedTypeName + "</color></b>\n" +
                "<b><color=#FF6666>► Provided:</color><color=#FFEFD5> " + providedTypeName + "</color></b>",
                "Execute parameter " + index + " of " + GetType().Name + " expects " + expectedTypeName +
                " and the signal carried " + providedTypeName + ".");
        }

        private void LogDetachedCall(string methodName)
        {
            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► " + methodName + " on a command that is not running!</color></b>\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Reason:</color><color=#FFEFD5> its group already finished, or " +
                methodName + " was called twice.</color></b>",
                methodName + " was called on " + GetType().Name + " after its group finished.");
        }
    }
}