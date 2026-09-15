#if UNITY_EDITOR
using System.Collections.Generic;
using Modules.AssetDeliveryModule.Enums;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>One Addressables group that carries the Play Asset Delivery schema, with the bundles the last build made for it.</summary>
    internal class AssetDeliveryGroupEVO
    {
        public string Group;
        public AssetPackPolicy Policy;
        public List<string> Bundles = new();
    }
}
#endif
