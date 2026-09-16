using System;

namespace FlowIoC.BaseModule.Signals
{
    public abstract class SignalBody : ISignalBody
    {
        /// <inheritdoc cref="ISignalBody.RemoveAllListeners"/>
        public abstract void RemoveAllListeners();

        /// <inheritdoc cref="ISignalBody.PayloadTypes"/>
        public abstract Type[] PayloadTypes { get; }

        /// <inheritdoc cref="ISignalBody.DispatchUntyped"/>
        public abstract void DispatchUntyped(params object[] args);

        protected Action<ISignalBody, object[]> _internalCallback;

        Action<ISignalBody, object[]> ISignalBody.InternalCallback
        {
            get => _internalCallback;
            set => _internalCallback = value;
        }

        protected string _name;

        public string Name => _name;

        void ISignalBody.SetName(string name) => _name = name;

        protected bool _hideCommandLog;

        bool ISignalBody.HideCommandLog
        {
            get => _hideCommandLog;
            set => _hideCommandLog = value;
        }

        protected bool _isFrameworkOwned;

        bool ISignalBody.IsFrameworkOwned
        {
            get => _isFrameworkOwned;
            set => _isFrameworkOwned = value;
        }

        private event Action<object[]> _untypedCallback;

        public void AddListenerUntyped(Action<object[]> listener) => _untypedCallback += listener;

        public void RemoveListenerUntyped(Action<object[]> listener) => _untypedCallback -= listener;

        /// <summary>Called by each arity's Dispatch with the boxed payload; empty for none.</summary>
        protected void InvokeUntyped(object[] args) => _untypedCallback?.Invoke(args);

        /// <summary>Each arity's RemoveAllListeners drops the untyped ones through this.</summary>
        protected void ClearUntypedListeners() => _untypedCallback = null;

        /// <summary>
        /// The one check every DispatchUntyped makes before it casts. Reported with the signal's
        /// name and both counts, because the caller holding an ISignalBody has nothing else to go on.
        /// </summary>
        protected void RequireArgumentCount(object[] args, int expected)
        {
            int actual = args?.Length ?? 0;

            if (actual == expected) return;

            throw new ArgumentException(
                "Signal '" + _name + "' carries " + expected + " payload(s), but DispatchUntyped was given " + actual + ".");
        }
    }
}