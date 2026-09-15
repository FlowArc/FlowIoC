using System;
using System.Collections.Generic;
using Modules.AssetDeliveryModule.Enums;

namespace Modules.AssetDeliveryModule.Data.ValueObjects
{
    /// <summary>One pack as the build wrote it: the store's name for it, the group it came from, its bundles.</summary>
    [Serializable]
    public class AssetPackVO
    {
        public string Pack;
        public string Group;
        public AssetPackPolicy Policy;
        public List<string> Bundles = new();
    }
}
