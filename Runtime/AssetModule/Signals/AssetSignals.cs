using FlowIoC.BaseModule.Signals;

namespace FlowIoC.AssetModule.Signals
{
    public class AssetSignals : ISignalHolder
    {
        public AssetSignalsIncoming Incoming = new();
        public AssetSignalsOutgoing Outgoing = new();

        public class AssetSignalsIncoming
        {
            // label is also used as the runtime groupId
            public Signal<string> LoadGroupByLabel = new();
            public Signal<string> ReleaseGroup = new();
            public Signal<string> ReleaseAsset = new();
        }

        public class AssetSignalsOutgoing
        {
            public Signal<string> GroupLoaded = new();
            public Signal<string> GroupReleased = new();
            public Signal<string> AssetLoadFailed = new();
        }
    }
}
