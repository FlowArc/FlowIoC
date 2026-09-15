#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Modules.AssetDeliveryModule.Constants;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>FlowAssetPacks.json, the same shape the runtime reads.</summary>
    internal class AssetPackManifestWriter
    {
        public string ToJson(IReadOnlyList<AssetPackVO> packs)
        {
            var manifest = new AssetPackManifestVO();
            manifest.Packs.AddRange(packs);
            return JsonUtility.ToJson(manifest, true);
        }

        public string Write(string directory, IReadOnlyList<AssetPackVO> packs)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, AssetDeliveryConstants.MANIFEST_FILE);
            File.WriteAllText(path, ToJson(packs));
            return path;
        }
    }
}
#endif
