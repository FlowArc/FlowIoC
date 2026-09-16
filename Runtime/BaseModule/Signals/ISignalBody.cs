using System;

namespace FlowIoC.BaseModule.Signals
{
    public interface ISignalBody
    {
        /// <summary>
        /// The path the holder stamped when it was bound - <c>PlayerSignals.Incoming.AddCurrency</c> -
        /// or the field name the constructor recorded before that. Public to read, because a tool
        /// that lists signals needs the name; written only by the package.
        /// </summary>
        string Name { get; }

        internal void SetName(string name);

        internal bool HideCommandLog { get; set; }

        /// <summary>
        /// Whether this signal belongs to the framework rather than to the game. Set once when the
        /// holder is bound, and read on every dispatch to choose the channel the line goes on.
        /// </summary>
        internal bool IsFrameworkOwned { get; set; }

        internal Action<ISignalBody, object[]> InternalCallback { get; set; }

        /// <summary>
        /// The payload types in order: empty for a <c>Signal</c>, one for a <c>Signal&lt;T1&gt;</c>,
        /// and so on. What a tool reads to know what to parse before it calls DispatchUntyped.
        /// </summary>
        Type[] PayloadTypes { get; }

        /// <summary>
        /// Dispatches with a boxed payload, one argument per payload type, cast to it. The door a
        /// tool uses when it holds the signal as an <c>ISignalBody</c> and cannot name <c>T</c> -
        /// the on-device debug panel firing a signal it found by reflection. Nothing generic is
        /// instantiated on the way, so an IL2CPP player has the code. An argument count other than
        /// the payload count throws <see cref="ArgumentException"/> naming the signal.
        /// </summary>
        void DispatchUntyped(params object[] args);

        /// <summary>
        /// Hears every dispatch with the payload boxed, in the same array the command binder gets -
        /// empty for a payload-less signal. A tool that shows "the last value this signal carried"
        /// listens here; game code adds the typed listener.
        /// </summary>
        void AddListenerUntyped(Action<object[]> listener);

        void RemoveListenerUntyped(Action<object[]> listener);

        /// <summary>
        /// Drops every listener this signal holds, once-listeners and untyped ones included,
        /// whatever the signal's arity - which is why it is declared here rather than on each
        /// <c>ISignal</c>. A Mediator's <c>OnRemove</c> is otherwise the only teardown path there is,
        /// so a signal that outlives the objects listening to it keeps every listener a destroyed
        /// one left behind.
        ///
        /// What a dispatch runs in the middle - the commands bound to the signal - is not touched.
        /// That is the binding's business, and the context that made it takes it back when it goes.
        /// </summary>
        void RemoveAllListeners();
    }
}