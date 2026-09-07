using System;
using System.Runtime.CompilerServices;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Signals
{
    public class Signal<T1, T2, T3, T4> : SignalBody, ISignal<T1, T2, T3, T4>
    {
        private event Action<T1, T2, T3, T4> _callbackOnce;
        private event Action<T1, T2, T3, T4> _callback;

        public Signal(bool hideCommandLog = false, [CallerMemberName] string name = "")
        {
            _name = name;
            _hideCommandLog = hideCommandLog;
        }

        public void AddListenerOnce(Action<T1, T2, T3, T4> listener)
        {
            _callbackOnce += listener;
        }

        public void AddListener(Action<T1, T2, T3, T4> listener)
        {
            _callback += listener;
        }

        public void RemoveListener(Action<T1, T2, T3, T4> listener)
        {
            _callback -= listener;
        }

        /// <summary>
        /// Takes back a listener added with AddListenerOnce. Without this a one-shot listener could
        /// not be dropped at all, so a Mediator that added one and went back to the pool before the
        /// signal ever came kept it, and answered a dispatch it no longer had anything to do with.
        /// </summary>
        public void RemoveListenerOnce(Action<T1, T2, T3, T4> listener)
        {
            _callbackOnce -= listener;
        }

        public void Dispatch(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (!_hideCommandLog)
                FlowLogger.Log(SystemLogType.Signal, "Signal is dispatched: '", _name, "' with 4 parameters!");
            // Taken off the signal before it runs, not after. A once-listener that adds another one
            // - or adds itself back - was writing into a field the next line then cleared, so the
            // listener it added was never heard from.
            Action<T1, T2, T3, T4> once = _callbackOnce;
            _callbackOnce = null;
            once?.Invoke(param1, param2, param3, param4);

            _internalCallback?.Invoke(this, new[]
            {
                param1 as object,
                param2 as object,
                param3 as object,
                param4 as object
            });
            _callback?.Invoke(param1, param2, param3, param4);
        }
    }

    public interface ISignal<T1, T2, T3, T4> : ISignalBody
    {
        void AddListenerOnce(Action<T1, T2, T3, T4> listener);
        void AddListener(Action<T1, T2, T3, T4> listener);
        void RemoveListener(Action<T1, T2, T3, T4> listener);
        void RemoveListenerOnce(Action<T1, T2, T3, T4> listener);
        void Dispatch(T1 param1, T2 param2, T3 param3, T4 param4);
    }
}