#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Addressables;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The Art an update brought, addressed the way an install addresses it: under its own name,
    /// in its screen's group. The files alone are not enough. A file the package renamed arrives
    /// under its new name with the old GUID, so the Addressables entry follows the file and keeps
    /// the old address - Loading 1.0.4 renamed T_LoadingBackground to SPR_LoadingBackground, and
    /// the loading screen then asked for an address nothing had.
    ///
    /// The address is changed only where it is the name of a file this same update removed:
    /// that is the name the install gave it. An address that is anything else was chosen in the
    /// game, and an update is not a reason to take that back - the rule Rename Module keeps.
    /// An Art file the update brought that is not addressable at all is registered, as the
    /// install would have.
    /// </summary>
    internal class ModuleUpdateArtAddresses
    {
        private readonly ScreenAddressableEntries _entries = new ScreenAddressableEntries();
        private readonly Func<string, string> _addressOf;
        private readonly Action<ScreenAddressableEntry> _register;
        private readonly Action<string, string> _readdress;

        internal ModuleUpdateArtAddresses() : this(AddressOf, Register, Readdress)
        {
        }

        internal ModuleUpdateArtAddresses(Func<string, string> addressOf, Action<ScreenAddressableEntry> register,
            Action<string, string> readdress)
        {
            _addressOf = addressOf;
            _register = register;
            _readdress = readdress;
        }

        /// <summary>
        /// <paramref name="moduleAssetPath"/> is the installed module's folder, project relative
        /// ("Assets/Modules/LoadingModule"); the plan's paths are relative to it. One line per
        /// entry touched comes back, for whoever reports the update.
        /// </summary>
        internal IReadOnlyList<string> Follow(ModuleUpdatePlanEVO plan, string moduleAssetPath)
        {
            var lines = new List<string>();

            if (plan == null || string.IsNullOrEmpty(moduleAssetPath))
                return lines;

            var removed = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in plan.Of(ModuleUpdateVerdict.Delete))
            {
                if (IsArtFile(path))
                    removed.Add(Path.GetFileNameWithoutExtension(path));
            }

            foreach (string path in plan.Of(ModuleUpdateVerdict.Copy))
            {
                if (!IsArtFile(path))
                    continue;

                string assetPath = moduleAssetPath.TrimEnd('/') + "/" + path;
                string screen = _entries.ScreenOfArtFolder(Path.GetDirectoryName(assetPath));

                if (screen == null)
                    continue;

                ScreenAddressableEntry wanted = _entries.ForArt(screen, assetPath);
                string current = _addressOf(assetPath);

                if (current == null)
                {
                    _register(wanted);
                    lines.Add($"Addressable {wanted.Address} registered in {wanted.GroupName}");
                }
                else if (current != wanted.Address && removed.Contains(current))
                {
                    _readdress(assetPath, wanted.Address);
                    lines.Add($"Addressable {current} → {wanted.Address}");
                }
            }

            if (lines.Count > 0)
                AssetDatabase.SaveAssets();

            return lines;
        }

        private static bool IsArtFile(string path) =>
            !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "Art", StringComparison.Ordinal);

        private static string AddressOf(string assetPath)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (settings == null || string.IsNullOrEmpty(guid))
                return null;

            return settings.FindAssetEntry(guid)?.address;
        }

        private static void Register(ScreenAddressableEntry entry) => new ScreenAddressables().Register(entry);

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
