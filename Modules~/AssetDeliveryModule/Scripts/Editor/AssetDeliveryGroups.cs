#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Modules.AssetDeliveryModule.Enums;
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets.Android;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// The groups a build delivers through the store: every group carrying the Play Asset
    /// Delivery schema, with the bundle files the build result says it produced. A group with
    /// the schema set to None is not delivered and is left out.
    /// </summary>
    internal class AssetDeliveryGroups
    {
        public List<AssetDeliveryGroupEVO> Read(AddressableAssetSettings settings, AddressablesPlayerBuildResult result)
        {
            var groups = new List<AssetDeliveryGroupEVO>();

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || !group.HasSchema<PlayAssetDeliverySchema>()) continue;

                DeliveryType delivery = group.GetSchema<PlayAssetDeliverySchema>().AssetPackDeliveryType;
                if (delivery == DeliveryType.None) continue;

                var evo = new AssetDeliveryGroupEVO { Group = group.Name, Policy = Policy(delivery) };

                foreach (AddressablesPlayerBuildResult.BundleBuildResult bundle in result.AssetBundleBuildResults)
                    if (bundle.SourceAssetGroup == group)
                        evo.Bundles.Add(Path.GetFileName(bundle.FilePath));

                groups.Add(evo);
            }

            return groups;
        }

        private AssetPackPolicy Policy(DeliveryType delivery) => delivery switch
        {
            DeliveryType.InstallTime => AssetPackPolicy.InstallTime,
            DeliveryType.OnDemand => AssetPackPolicy.OnDemand,
            _ => AssetPackPolicy.FastFollow
        };
    }
}
#endif
