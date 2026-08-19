using System;

namespace FlowIoC.BaseModule.Signals
{
    public interface ISignalBody
    {
        internal string Name { get; set; }

        internal bool HideCommandLog { get; set; }

        internal Action<ISignalBody, object[]> InternalCallback { get; set; }
    }
}