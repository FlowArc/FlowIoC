#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.LoadingModule.LoadingTestModule.Signals
{
    internal class LoadingTestInternalSignals : ISignalHolder
    {
        public Signal Launch = new();
        public Signal RunBackground = new();
        public Signal RunDownload = new();
        public Signal Retry = new();
    }
}
#endif
