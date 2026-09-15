#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// Runs after every Addressables build. Android and iOS get the manifest beside the catalog;
    /// iOS also gets its packs folded out of the build. What fails quietly otherwise is reported
    /// here as an error: Play Asset Delivery not initialised, an APK instead of an App Bundle, a
    /// binary that is not split, two groups sanitising to one iOS pack name.
    /// </summary>
    internal class AssetDeliveryBuildHook
    {
        private const string IOS_PACKS = "ServerData/iOS/AssetPacks";
        private const string ANDROID_PACK_DATA = "Assets/PlayAssetDelivery/Build/aa/CustomAssetPacksData.json";

        [InitializeOnLoadMethod]
        private static void Register() => BuildScript.buildCompleted += result => new AssetDeliveryBuildHook().OnBuildCompleted(result);

        internal void OnBuildCompleted(AddressableAssetBuildResult result)
        {
            if (result is not AddressablesPlayerBuildResult playerResult || !string.IsNullOrEmpty(result.Error)) return;

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (target != BuildTarget.Android && target != BuildTarget.iOS) return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AssetDeliveryGroupEVO> groups = new AssetDeliveryGroups().Read(settings, playerResult);
            var names = new AssetPackNames();
            var packs = new List<AssetPackVO>();

            if (target == BuildTarget.Android)
            {
                if (!ReadAndroid(groups, names, packs)) return;
            }
            else
            {
                if (!ReadIos(groups, names, packs)) return;

                new IosAssetPackPackager(runXcrun: true).Package(Addressables.BuildPath, packs, IOS_PACKS);
                new AssetDeliverySetup().RegisterInitializationObject(settings);
            }

            string path = new AssetPackManifestWriter().Write(Addressables.BuildPath, packs);
            FlowLogger.Log($"AssetDeliveryBuildHook - {packs.Count} pack(s) written to {path}");
        }

        private bool ReadAndroid(List<AssetDeliveryGroupEVO> groups, AssetPackNames names, List<AssetPackVO> packs)
        {
            if (groups.Count == 0) return true;

            if (!File.Exists(ANDROID_PACK_DATA))
            {
                FlowLogger.LogError("AssetDeliveryBuildHook - no CustomAssetPacksData.json: run Window > Asset Management > Addressables > Init Play Asset Delivery and build with the Play Asset Delivery script, or no pack reaches Play.");
                return false;
            }

            IReadOnlyDictionary<string, string> bundleToPack = names.AndroidBundleToPack(File.ReadAllText(ANDROID_PACK_DATA));

            foreach (AssetDeliveryGroupEVO group in groups)
            {
                string pack = null;

                foreach (string bundle in group.Bundles)
                    if (bundleToPack.TryGetValue(Path.GetFileNameWithoutExtension(bundle), out pack))
                        break;

                if (pack == null)
                {
                    FlowLogger.LogError($"AssetDeliveryBuildHook - group '{group.Group}' is in no Android asset pack; it was not built with the Play Asset Delivery script.");
                    continue;
                }

                packs.Add(new AssetPackVO { Pack = pack, Group = group.Group, Policy = group.Policy, Bundles = group.Bundles });
            }

            if (!EditorUserBuildSettings.buildAppBundle)
                FlowLogger.LogError("AssetDeliveryBuildHook - Build App Bundle is off: an APK carries no asset packs, and every delivery group is inlined into StreamingAssets.");

            if (!PlayerSettings.Android.splitApplicationBinary)
                FlowLogger.LogError("AssetDeliveryBuildHook - Split Application Binary is off: Unity generates asset packs only when it is on.");

            return true;
        }

        private bool ReadIos(List<AssetDeliveryGroupEVO> groups, AssetPackNames names, List<AssetPackVO> packs)
        {
            var groupNames = new List<string>();
            foreach (AssetDeliveryGroupEVO group in groups) groupNames.Add(group.Group);

            IReadOnlyList<string> collisions = names.Collisions(groupNames);

            if (collisions.Count > 0)
            {
                FlowLogger.LogError($"AssetDeliveryBuildHook - two delivery groups sanitise to one iOS pack id: {string.Join(", ", collisions)}. Rename one of them.");
                return false;
            }

            foreach (AssetDeliveryGroupEVO group in groups)
                packs.Add(new AssetPackVO { Pack = names.Sanitize(group.Group), Group = group.Group, Policy = group.Policy, Bundles = group.Bundles });

            return true;
        }
    }
}
#endif
