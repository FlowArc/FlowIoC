using System;
using System.Collections.Generic;

namespace Modules.AssetDeliveryModule.Data.ValueObjects
{
    /// <summary>FlowAssetPacks.json - the packs the build declared.</summary>
    [Serializable]
    public class AssetPackManifestVO
    {
        public List<AssetPackVO> Packs = new();
    }
}
