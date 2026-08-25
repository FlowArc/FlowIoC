#if UNITY_EDITOR
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The AssetDatabase behind an interface, so ModuleRegistry can be exercised in a test
    /// without folders having to exist in the project running it.
    /// </summary>
    internal interface IAssetPaths
    {
        string GuidOf(string assetPath);
        string PathOf(string guid);
    }

    internal class AssetDatabasePaths : IAssetPaths
    {
        public string GuidOf(string assetPath) =>
            AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets);

        public string PathOf(string guid) => AssetDatabase.GUIDToAssetPath(guid);
    }

    internal class ModuleIndexProvider
    {
        private readonly FlowIoCProjectPaths _paths = new FlowIoCProjectPaths();

        private readonly ModuleIndexIgnoreRule _ignoreRule = new ModuleIndexIgnoreRule();

        public FlowIoCModuleIndex LoadOrCreate()
        {
            var index = AssetDatabase.LoadAssetAtPath<FlowIoCModuleIndex>(_paths.ModuleIndex);
            if (index != null)
            {
                // Also here, not only on the create path below, so a project that already had the
                // asset before this rule existed picks it up on its next Editor session rather
                // than only after someone deletes the index.
                _ignoreRule.Ensure();
                return index;
            }

            // Recreating this asset costs a rescan and nothing else. Unlike FlowConsoleSettings
            // there is no user data in it to overwrite, which is why "could not load" and
            // "does not exist" may be treated the same here and nowhere else.
            EnsureDirectory();

            // Before the asset exists, so it is never stageable without the rule already in place.
            _ignoreRule.Ensure();

            index = ScriptableObject.CreateInstance<FlowIoCModuleIndex>();
            AssetDatabase.CreateAsset(index, _paths.ModuleIndex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return index;
        }

        private void EnsureDirectory()
        {
            string directory = Path.GetDirectoryName(_paths.ModuleIndex);
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory)) return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}

#endif