#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Inside a test module the asset folders sit under Editor/. Test code never reaches a player
    /// build, and an Editor folder is what makes that true of its assets too: a Resources folder
    /// anywhere else is shipped whether or not anything loads it, and its prefabs would arrive with
    /// scripts that #if UNITY_EDITOR compiled away.
    ///
    /// So at the root of a test module, or of a screen made for one, only Scripts, Scenes, Editor
    /// and the module containers belong. Any other folder is moved under Editor/ - through the
    /// AssetDatabase, so every reference to what it holds survives. When Editor/ already has a
    /// folder of that name, the two are merged child by child; a child that exists on both sides
    /// is never written over, and is left for a person to choose between.
    ///
    /// It runs before MandatoryFoldersCheck, which would otherwise make an empty Editor/Prefabs
    /// beside the full Prefabs this is about to move.
    /// </summary>
    internal class TestTreeAssetFoldersCheck : IModuleCheck
    {
        private const string EDITOR_FOLDER = "Editor";

        private readonly Func<string, IEnumerable<string>> _rootFoldersOf;
        private readonly Func<string, bool> _folderExists;
        private readonly Func<string, IEnumerable<string>> _childrenOf;
        private readonly Func<string, string, string> _moveAsset;
        private readonly Action<string> _deleteFolder;
        private readonly Action<string> _createFolder;
        private readonly Func<string, bool> _holdsAddressables;
        private readonly Func<string, bool> _shippedThere;

        internal TestTreeAssetFoldersCheck() : this(
            RootFoldersOf,
            path => AssetDatabase.IsValidFolder(path) || File.Exists(path),
            ChildrenOf,
            AssetDatabase.MoveAsset,
            path => AssetDatabase.DeleteAsset(path),
            CreateFolder,
            HoldsAddressables,
            ShippedThere)
        {
        }

        internal TestTreeAssetFoldersCheck(
            Func<string, IEnumerable<string>> rootFoldersOf,
            Func<string, bool> folderExists,
            Func<string, IEnumerable<string>> childrenOf,
            Func<string, string, string> moveAsset,
            Action<string> deleteFolder,
            Action<string> createFolder,
            Func<string, bool> holdsAddressables = null,
            Func<string, bool> shippedThere = null)
        {
            _rootFoldersOf = rootFoldersOf;
            _folderExists = folderExists;
            _childrenOf = childrenOf;
            _moveAsset = moveAsset;
            _deleteFolder = deleteFolder;
            _createFolder = createFolder;
            _holdsAddressables = holdsAddressables ?? (_ => false);
            _shippedThere = shippedThere ?? (_ => false);
        }

        public string Id => "test-tree-assets";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            List<string> misplaced = Misplaced(module);

            if (misplaced.Count == 0)
                return FindingEVO.Ok(Id, "Test assets under Editor");

            foreach (string folder in misplaced)
            {
                string clash = Clashes(module, folder).FirstOrDefault();

                if (clash != null)
                {
                    return FindingEVO.Manual(Id,
                        $"{Path.GetFileName(folder)}/{Path.GetFileName(clash)} is under Editor/{Path.GetFileName(folder)} "
                        + "as well - keep one and delete the other, then scan again.", folder);
                }
            }

            return FindingEVO.Fixable(Id,
                $"{string.Join(", ", misplaced.Select(Path.GetFileName))} at a test module's root can reach a player "
                + "build. Fix moves them under Editor/.", misplaced[0]);
        }

        public void Fix(ModuleTargetEVO module)
        {
            List<string> misplaced = Misplaced(module);

            if (misplaced.Count == 0)
                return;

            string editor = Combine(module.AssetPath, EDITOR_FOLDER);

            if (!_folderExists(editor))
                _createFolder(editor);

            foreach (string folder in misplaced)
            {
                string twin = Combine(editor, Path.GetFileName(folder));

                if (!_folderExists(twin))
                {
                    Report(folder, _moveAsset(folder, twin));
                    continue;
                }

                // A child whose name is taken under Editor/ stays where it is, and so does one
                // whose move was refused; the folder goes only once nothing is left in it.
                foreach (string child in _childrenOf(folder).ToList())
                {
                    string target = Combine(twin, Path.GetFileName(child));

                    if (!_folderExists(target))
                        Report(child, _moveAsset(child, target));
                }

                if (!_childrenOf(folder).Any())
                    _deleteFolder(folder);
            }
        }

        /// <summary>
        /// A move the AssetDatabase refused, named with the asset and the reason, so whatever was
        /// left behind is found rather than taken for moved.
        /// </summary>
        private static void Report(string asset, string error)
        {
            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogWarning($"<color=cyan>[FlowIoC]</color> Test assets under Editor - '{asset}' was not moved: {error}");
        }

        private List<string> Misplaced(ModuleTargetEVO module)
        {
            if (module == null || !module.InTestTree || string.IsNullOrEmpty(module.AssetPath))
                return new List<string>();

            return (_rootFoldersOf(module.AssetPath) ?? Enumerable.Empty<string>())
                .Select(Normalize)
                .Where(folder => !StaysAtRoot(Path.GetFileName(folder)))
                // Addressables takes no asset from an Editor folder, so a folder holding an entry -
                // the Audio sample's clips, loaded through AssetReference - stays where it is.
                .Where(folder => !_holdsAddressables(folder))
                // A folder an installed module ships where it is stays there: the package decides
                // its own layout, and a copy moved away would come back beside the moved one, with
                // the same GUIDs, on the next update. Asked of the record rather than the project,
                // so an entry missing from Addressables does not move it either.
                .Where(folder => !_shippedThere(folder))
                .ToList();
        }

        /// <summary>
        /// Whether the shipped record of the installed module this folder sits in lists a file
        /// under it. The record lives beside the installed module's card, so the walk goes up from
        /// the folder to the first one holding a record, and never past Assets.
        /// </summary>
        private static bool ShippedThere(string folderAssetPath)
        {
            var record = new ShippedRecord();
            string folder = Normalize(folderAssetPath).TrimEnd('/');
            string owner = folder;

            while (owner.Contains("/"))
            {
                owner = owner.Substring(0, owner.LastIndexOf('/'));

                if (!File.Exists(Path.Combine(owner, ShippedRecord.FILE_NAME)))
                    continue;

                ShippedRecordEVO shipped = record.Read(owner);
                string prefix = folder.Substring(owner.Length + 1) + "/";

                return shipped != null && shipped.Files.Keys.Any(path => path.StartsWith(prefix, StringComparison.Ordinal));
            }

            return false;
        }

        private static bool HoldsAddressables(string folderAssetPath)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            if (settings == null || !AssetDatabase.IsValidFolder(folderAssetPath))
                return false;

            return AssetDatabase.FindAssets(string.Empty, new[] {folderAssetPath})
                .Any(guid => settings.FindAssetEntry(guid) != null);
        }

        private IEnumerable<string> Clashes(ModuleTargetEVO module, string folder)
        {
            string twin = Combine(Combine(module.AssetPath, EDITOR_FOLDER), Path.GetFileName(folder));

            if (!_folderExists(twin))
                return Enumerable.Empty<string>();

            return _childrenOf(folder).Where(child => _folderExists(Combine(twin, Path.GetFileName(child))));
        }

        /// <summary>
        /// Scripts, Scenes and Editor stay, and so does every module container - zSubModules,
        /// zScreenModules, zTestModules, whatever a project renamed them to, all of which start
        /// with the z that keeps them at the bottom of the Project window.
        /// </summary>
        private static bool StaysAtRoot(string name) =>
            name == "Scripts" || name == "Scenes" || name == EDITOR_FOLDER || name.StartsWith("z", StringComparison.Ordinal);

        private static string Combine(string left, string right) => Normalize(left).TrimEnd('/') + "/" + right;

        private static string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/');

        private static IEnumerable<string> RootFoldersOf(string assetPath) =>
            AssetDatabase.GetSubFolders(assetPath);

        private static IEnumerable<string> ChildrenOf(string assetPath)
        {
            if (!Directory.Exists(assetPath))
                return Enumerable.Empty<string>();

            return Directory.GetFileSystemEntries(assetPath)
                .Where(entry => !entry.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(Normalize);
        }

        private static void CreateFolder(string assetPath)
        {
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }
    }
}
#endif