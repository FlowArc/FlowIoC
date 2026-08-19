using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;

namespace FlowIoC.AssetModule.Model
{
    internal interface IAssetRegistryModel
    {
        Dictionary<string, AssetEntryVO> Entries { get; }
        Dictionary<string, AssetGroupVO> Groups { get; }
        Dictionary<string, Task<object>> InFlight { get; }

        AssetEntryVO GetOrCreateEntry(string registryKey);
        AssetGroupVO GetOrCreateGroup(string groupId);
        void Clear();
    }
}
