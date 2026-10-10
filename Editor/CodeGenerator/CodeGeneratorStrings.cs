#if UNITY_EDITOR
using FlowIoC.BaseModule.ProjectPaths;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorStrings
    {
        // The package root is resolved from this assembly instead of being hardcoded, so the
        // generator keeps working however the package was installed: embedded under Packages/,
        // pulled from a Git URL into Library/PackageCache, or resolved from a registry.
        private static readonly PackageInfo _package =
            PackageInfo.FindForAssembly(typeof(CodeGeneratorStrings).Assembly);

        // Unity virtual path, e.g. "Packages/com.birrstudio.flowioc.core". Used with AssetDatabase.
        private static readonly string _packageAssetRoot =
            _package != null ? _package.assetPath : "Packages/com.birrstudio.flowioc.core";

        // Absolute path on disk. Used with System.IO when reading the code templates.
        private static readonly string _packageDiskRoot =
            _package != null ? _package.resolvedPath : Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC";

        // One instance for the whole type: the paths object is stateless, and this class is
        // already the package's static string table.
        private static readonly FlowIoCProjectPaths _paths = new FlowIoCProjectPaths();

        public static readonly string CONFIG_PATH = _paths.CodeGeneratorSettings;

        public static readonly string SCREEN_SERVICE_ROOT_PATH = _packageAssetRoot + "/Assets/Prefabs/ScreenServiceRoot.prefab";
        public static readonly string ASSET_SERVICE_ROOT_PATH = _packageAssetRoot + "/Assets/Prefabs/AssetServiceRoot.prefab";
        internal static readonly string SCREEN_MANAGER_PREFAB_PATH = _packageAssetRoot + "/Assets/Prefabs/ScreenManager.prefab";


        internal static readonly string TempViewPath = _packageDiskRoot + "/Editor/CodeGenerator/TempViews/TempView.cs";
        internal static readonly string TempMediatorPath = _packageDiskRoot + "/Editor/CodeGenerator/TempViews/TempMediator.cs";

        internal static readonly string TempModelPath = _packageDiskRoot + "/Editor/CodeGenerator/TempModels/TempModel.cs";
        internal static readonly string TempIModelPath = _packageDiskRoot + "/Editor/CodeGenerator/TempModels/ITempModel.cs";

        internal static readonly string TempCommandPath = _packageDiskRoot + "/Editor/CodeGenerator/TempCommands/TempCommand.cs";

        internal static readonly string TempSignalsPath = _packageDiskRoot + "/Editor/CodeGenerator/TempSignals/TempSignals.cs";

        internal static readonly string TempInternalSignalsPath =
            _packageDiskRoot + "/Editor/CodeGenerator/TempSignals/TempInternalSignals.cs";

        internal static readonly string TempContextPath = _packageDiskRoot + "/Editor/CodeGenerator/TempRoots/TempContext.cs";
        internal static readonly string TempRootPath = _packageDiskRoot + "/Editor/CodeGenerator/TempRoots/TempRoot.cs";


        internal static readonly string TempScreenViewPath = _packageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenView.cs";
        internal static readonly string TempScreenMediatorPath = _packageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenMediator.cs";


        internal static readonly string TempScreenTestContextPath = _packageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenTestContext.cs";
        internal static readonly string TempScreenTestRootPath = _packageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenTestRoot.cs";

        internal static string GetPath(string path, string parentFolderName)
        {
            return string.IsNullOrEmpty(parentFolderName)
                ? path.Replace("$", "Runtime")
                : path.Replace("$", parentFolderName);
        }
    }
}
#endif