using System;

namespace FlowIoC.BaseModule.Signals
{
    public interface ISignalBody
    {
        internal string Name { get; set; }

        internal bool HideCommandLog { get; set; }

        internal Action<ISignalBody, object[]> InternalCallback { get; set; }

        /// <summary>
        /// Drops every listener this signal holds, once-listeners included, whatever the signal's
        /// arity - which is why it is declared here rather than on each <c>ISignal</c>. A Mediator's
        /// <c>OnRemove</c> is otherwise the only teardown path there is, so a signal that outlives
        /// the objects listening to it keeps every listener a destroyed one left behind.
        ///
        /// What a dispatch runs in the middle - the commands bound to the signal - is not touched.
        /// That is the binding's business, and the context that made it takes it back when it goes.
        /// </summary>
        void RemoveAllListeners();
    }
}