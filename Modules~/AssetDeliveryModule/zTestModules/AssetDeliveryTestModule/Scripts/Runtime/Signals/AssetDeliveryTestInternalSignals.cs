#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.AssetDeliveryModule.AssetDeliveryTestModule.Signals
{
    public class AssetDeliveryTestInternalSignals : ISignalHolder
    {
        /// <summary>The boot, and the retry runs it again.</summary>
        public Signal Launch = new();
    }
}
#endif
