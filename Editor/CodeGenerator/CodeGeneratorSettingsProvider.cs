#if UNITY_EDITOR
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// The code generator settings asset, made when it is missing the way ModuleIndexProvider
    /// makes the index. A missing asset is a project FlowIoC has not set up yet, not a fault: on a
    /// first open nothing has made it - the generator menus create it, and nobody has opened one -
    /// and the startup rebuild of the module index runs before the setup install gets its turn.
    /// Made here with its defaults, it is the same asset the install would have made a few
    /// seconds later.
    /// </summary>
    internal class CodeGeneratorSettingsProvider
    {
        private readonly FlowIoCProjectPaths _paths;

        internal CodeGeneratorSettingsProvider() : this(new FlowIoCProjectPaths())
        {
        }

        internal CodeGeneratorSettingsProvider(FlowIoCProjectPaths paths)
        {
            _paths = paths;
        }

        public ED_CodeGenerator LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(_paths.CodeGeneratorSettings);
            if (settings != null) return settings;

            EnsureDirectory();

            settings = ScriptableObject.CreateInstance<ED_CodeGenerator>();
            AssetDatabase.CreateAsset(settings, _paths.CodeGeneratorSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"ED_CodeGenerator asset created at: {_paths.CodeGeneratorSettings}");

            return settings;
        }

        private void EnsureDirectory()
        {
            string directory = Path.GetDirectoryName(_paths.CodeGeneratorSettings);
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory)) return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}

#endif
