using System;

namespace FlowIoC.BaseModule.Signals
{
    public class SignalBody : ISignalBody
    {
        protected Action<ISignalBody, object[]> _internalCallback;

        Action<ISignalBody, object[]> ISignalBody.InternalCallback
        {
            get => _internalCallback;
            set => _internalCallback = value;
        }

        protected string _name;

        string ISignalBody.Name
        {
            get => _name;
            set => _name = value;
        }

        protected bool _hideCommandLog;

        bool ISignalBody.HideCommandLog
        {
            get => _hideCommandLog;
            set => _hideCommandLog = value;
        }
    }
}