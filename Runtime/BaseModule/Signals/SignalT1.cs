using System;
using System.Runtime.CompilerServices;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Signals
{
    public class Signal<T1> : SignalBody, ISignal<T1>
    {
        private event Action<T1> _callbackOnce;
        private event Action<T1> _callback;

        public Signal(bool hideCommandLog = false, [CallerMemberName] string name = "")
        {
            _name = name;
            _hideCommandLog = hideCommandLog;
        }

        public void AddListenerOnce(Action<T1> listener)
        {
            _callbackOnce += listener;
        }

        public void AddListener(Action<T1> listener)
        {
            _callback += listener;
        }

        public void RemoveListener(Action<T1> listener)
        {
            _callback -= listener;
        }

        /// <summary>
        /// Takes back a listener added with AddListenerOnce. Without this a one-shot listener could
        /// not be dropped at all, so a Mediator that added one and went back to the pool before the
        /// signal ever came kept it, and answered a dispatch it no longer had anything to do with.
        /// </summary>
        public void RemoveListenerOnce(Action<T1> listener)
        {
            _callbackOnce -= listener;
        }

        /// <inheritdoc cref="ISignalBody.RemoveAllListeners"/>
        public override void RemoveAllListeners()
        {
            _callbackOnce = null;
            _callback = null;
        }

        public void Dispatch(T1 param)
        {
            if (!_hideCommandLog)
                FlowLogger.LogDispatch(_isFrameworkOwned, "'", _name, "' dispatched (1 parameter)");

            // Taken off the signal before it runs, not after. A once-listener that adds another one
            // - or adds itself back - was writing into a field the next line then cleared, so the
            // listener it added was never heard from.
            Action<T1> once = _callbackOnce;
            _callbackOnce = null;
            once?.Invoke(param);

            // One array per dispatch, and it stays that way. Pooling it was looked at and turned
            // down: the array does not die when this method returns. CommandBinder hands it to a
            // CommandGroupResolver, which holds it as _signalParameters until the run is disposed -
            // and a run with a retained step lasts as long as whatever that step is waiting on. A
            // sub group takes the same array again. Returning it to a pool therefore needs an owner
            // count across every resolver that took it, and getting that wrong hands a live payload
            // to somebody else's dispatch. That is a worse bug than one small array is a cost, and
            // nothing has measured the array as one.
            _internalCallback?.Invoke(this, new[]
            {
                param as object
            });
            _callback?.Invoke(param);
        }
    }

    public interface ISignal<T> : ISignalBody
    {
        void AddListenerOnce(Action<T> listener);
        void AddListener(Action<T> listener);
        void RemoveListener(Action<T> listener);
        void RemoveListenerOnce(Action<T> listener);
        void Dispatch(T param);
    }
}