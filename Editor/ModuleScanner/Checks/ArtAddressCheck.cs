#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A file in a screen module's Art folder that is addressable is addressed by its own name -
    /// the name the install gave it, and the name the screen asks for.
    ///
    /// When the two part, nothing says so until the screen loads the art: the load fails with
    /// "No Location found for Key" and the screen shows whatever it had bundled. A module update
    /// that renamed the file left exactly that behind before the updater learned to follow a
    /// rename, and a file renamed by hand does the same. An Art file that is not addressable at
    /// all is left alone here: whether it should be is the screen's business.
    ///
    /// The fix sets the address to the file's name.
    /// </summary>
    internal class ArtAddressCheck : IModuleCheck
    {
        private readonly Func<string, IEnumerable<string>> _artFilesOf;
        private readonly Func<string, string> _addressOf;
        private readonly Action<string, string> _readdress;

        internal ArtAddressCheck() : this(ArtFilesOf, AddressOf, Readdress)
        {
        }

        internal ArtAddressCheck(Func<string, IEnumerable<string>> artFilesOf, Func<string, string> addressOf,
            Action<string, string> readdress)
        {
            _artFilesOf = artFilesOf;
            _addressOf = addressOf;
            _readdress = readdress;
        }

        public string Id => "art-address";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            List<(string Path, string Address)> mismatched = Mismatched(module);

            if (mismatched.Count == 0)
                return FindingEVO.Ok(Id, "Art addresses");

            (string path, string address) = mismatched[0];
            string name = Path.GetFileNameWithoutExtension(path);

            return FindingEVO.Fixable(
                Id,
                $"{Path.GetFileName(path)} is addressed as \"{address}\", not by its own name, so a screen "
                + $"asking for \"{name}\" finds nothing. Fix sets the address to \"{name}\".",
                path);
        }

        public void Fix(ModuleTargetEVO module)
        {
            List<(string Path, string Address)> mismatched = Mismatched(module);

            foreach ((string path, _) in mismatched)
                _readdress(path, Path.GetFileNameWithoutExtension(path));

            if (mismatched.Count > 0)
                AssetDatabase.SaveAssets();
        }

        private List<(string Path, string Address)> Mismatched(ModuleTargetEVO module)
        {
            var mismatched = new List<(string, string)>();

            if (module == null || module.Kind != ModuleKind.Screen || string.IsNullOrEmpty(module.AssetPath))
                return mismatched;

            foreach (string path in _artFilesOf(module.AssetPath.TrimEnd('/') + "/Art") ?? Array.Empty<string>())
            {
                string address = _addressOf(path);

                if (address != null && address != Path.GetFileNameWithoutExtension(path))
                    mismatched.Add((path, address));
            }

            return mismatched;
        }

        private static IEnumerable<string> ArtFilesOf(string artFolder)
        {
            if (!AssetDatabase.IsValidFolder(artFolder))
                yield break;

            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] {artFolder}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!AssetDatabase.IsValidFolder(path) && Path.GetDirectoryName(path)?.Replace('\\', '/') == artFolder)
                    yield return path;
            }
        }

        private static string AddressOf(string assetPath)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            return settings == null || string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid)?.address;
        }

        private static void Readdress(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            AddressableAssetEntry entry = settings?.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

            if (entry == null)
                return;

            entry.SetAddress(address);
            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }
    }
}
#endif
