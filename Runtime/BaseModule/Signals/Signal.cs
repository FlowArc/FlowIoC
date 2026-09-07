using System;
using System.Runtime.CompilerServices;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Signals
{
    public class Signal : SignalBody, ISignal
    {
        private event Action _callbackOnce;
        private event Action _callback;

        public Signal(bool hideCommandLog = false, [CallerMemberName] string name = "")
        {
            _name = name;
            _hideCommandLog = hideCommandLog;
        }

        public void AddListenerOnce(Action listener)
        {
            _callbackOnce += listener;
        }

        public void AddListener(Action listener)
        {
            _callback += listener;
        }

        public void RemoveListener(Action listener)
        {
            _callback -= listener;
        }

        /// <summary>
        /// Takes back a listener added with AddListenerOnce. Without this a one-shot listener could
        /// not be dropped at all, so a Mediator that added one and went back to the pool before the
        /// signal ever came kept it, and answered a dispatch it no longer had anything to do with.
        /// </summary>
        public void RemoveListenerOnce(Action listener)
        {
            _callbackOnce -= listener;
        }

        public void Dispatch()
        {
            if (!_hideCommandLog)
                FlowLogger.Log(SystemLogType.Signal, $"Signal is dispatched: '{((ISignalBody) this).Name}' with 0 parameter!");
            // Taken off the signal before it runs, not after. A once-listener that adds another one
            // - or adds itself back - was writing into a field the next line then cleared, so the
            // listener it added was never heard from.
            Action once = _callbackOnce;
            _callbackOnce = null;
            once?.Invoke();

            _internalCallback?.Invoke(this, null);
            _callback?.Invoke();
        }
    }

    public interface ISignal : ISignalBody
    {
        void AddListenerOnce(Action listener);
        void AddListener(Action listener);
        void RemoveListener(Action listener);
        void RemoveListenerOnce(Action listener);
        void Dispatch();
    }
}