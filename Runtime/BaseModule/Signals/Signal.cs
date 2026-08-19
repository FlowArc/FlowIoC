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

        public void Dispatch()
        {
            if (!_hideCommandLog)
                FlowLogger.Log(SystemLogType.Signal, $"Signal is dispatched: '{((ISignalBody) this).Name}' with 0 parameter!");
            _callbackOnce?.Invoke();
            _callbackOnce = null;

            _internalCallback?.Invoke(this, null);
            _callback?.Invoke();
        }
    }

    public interface ISignal : ISignalBody
    {
        void AddListenerOnce(Action listener);
        void AddListener(Action listener);
        void RemoveListener(Action listener);
        void Dispatch();
    }
}