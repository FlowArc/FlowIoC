using System.Collections.Generic;

namespace FlowIoC.AssetModule.Data
{
    internal sealed class AssetGroupVO
    {
        public readonly HashSet<string> Keys = new();
        public bool IsLoaded;
    }
}
