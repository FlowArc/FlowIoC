#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    internal class ModuleIndexRebuilder
    {
        private readonly FlowIoCProjectPaths _paths = new FlowIoCProjectPaths();

        /// <summary>
        /// The rebuilt index, or null when the code generator settings could not be loaded and
        /// the index was left as it is. Returning it rather than leaving callers to load the
        /// index themselves is what keeps a failed rebuild distinguishable from a successful
        /// one: a caller that loaded it independently would get an empty index either way, and
        /// go on to write into it as though the rebuild had happened.
        /// </summary>
        public ED_ModuleIndex Rebuild()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(_paths.CodeGeneratorSettings);
            if (settings == null)
            {
                Debug.LogWarning("<color=cyan>FlowIoC:</color> the code generator settings could not be " +
                                 "loaded, so the module index was left as it is.");
                return null;
            }

            var resolver = new ModuleKindResolver(
                settings.FolderNameFor(FolderEVO.FolderType.SubModules, "zSubModules"),
                settings.FolderNameFor(FolderEVO.FolderType.ScreenModules, "zScreenModules"),
                settings.FolderNameFor(FolderEVO.FolderType.TestModules, "zTestModules"));

            var scanner = new ModuleTreeScanner(resolver);
            var scanned = new List<ScannedModule>();

            foreach (string modulesRoot in new ModuleScannerRoots().All(Path.GetDirectoryName(Application.dataPath)))
                scanned.AddRange(scanner.Scan(modulesRoot));

            ED_ModuleIndex index = new ModuleIndexProvider().LoadOrCreate();

            index.Replace(new ModuleIndexBuilder().Build(scanned, GuidOfAbsolutePath, index.Modules));

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();

            return index;
        }

        private string GuidOfAbsolutePath(string absolutePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (projectRoot == null) return string.Empty;

            string relative = Path.GetRelativePath(projectRoot, absolutePath).Replace('\\', '/');
            return AssetDatabase.AssetPathToGUID(relative);
        }
    }
}

#endif