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
        private static readonly PackageInfo Package =
            PackageInfo.FindForAssembly(typeof(CodeGeneratorStrings).Assembly);

        // Unity virtual path, e.g. "Packages/com.flowarc.flowioc.core". Used with AssetDatabase.
        private static readonly string PackageAssetRoot =
            Package != null ? Package.assetPath : "Packages/com.flowarc.flowioc.core";

        // Absolute path on disk. Used with System.IO when reading the code templates.
        private static readonly string PackageDiskRoot =
            Package != null ? Package.resolvedPath : Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC";

        // One instance for the whole type: the paths object is stateless, and this class is
        // already the package's static string table.
        private static readonly FlowIoCProjectPaths Paths = new FlowIoCProjectPaths();

        public static readonly string CONFIG_PATH = Paths.CodeGeneratorSettings;

        public static readonly string SCREEN_SERVICE_ROOT_PATH = PackageAssetRoot + "/Assets/Prefabs/ScreenServiceRoot.prefab";
        internal static readonly string SCREEN_MANAGER_PREFAB_PATH = PackageAssetRoot + "/Assets/Prefabs/ScreenManager.prefab";


        internal static readonly string TempViewPath = PackageDiskRoot + "/Editor/CodeGenerator/TempViews/TempView.cs";
        internal static readonly string TempMediatorPath = PackageDiskRoot + "/Editor/CodeGenerator/TempViews/TempMediator.cs";

        internal static readonly string TempModelPath = PackageDiskRoot + "/Editor/CodeGenerator/TempModels/TempModel.cs";
        internal static readonly string TempIModelPath = PackageDiskRoot + "/Editor/CodeGenerator/TempModels/ITempModel.cs";

        internal static readonly string TempCommandPath = PackageDiskRoot + "/Editor/CodeGenerator/TempCommands/TempCommand.cs";

        internal static readonly string TempSignalsPath = PackageDiskRoot + "/Editor/CodeGenerator/TempSignals/TempSignals.cs";

        internal static readonly string TempInternalSignalsPath =
            PackageDiskRoot + "/Editor/CodeGenerator/TempSignals/TempInternalSignals.cs";

        internal static readonly string TempContextPath = PackageDiskRoot + "/Editor/CodeGenerator/TempRoots/TempContext.cs";
        internal static readonly string TempRootPath = PackageDiskRoot + "/Editor/CodeGenerator/TempRoots/TempRoot.cs";


        internal static readonly string TempScreenViewPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenView.cs";
        internal static readonly string TempScreenMediatorPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenMediator.cs";

        internal static readonly string TempScreenContextPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenContext.cs";
        internal static readonly string TempScreenRootPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenRoot.cs";

        internal static readonly string TempScreenTestContextPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenTestContext.cs";
        internal static readonly string TempScreenTestRootPath = PackageDiskRoot + "/Editor/CodeGenerator/TempScreens/TempScreenTestRoot.cs";

        internal static string GetPath(string path, string parentFolderName)
        {
            return string.IsNullOrEmpty(parentFolderName)
                ? path.Replace("$", "Runtime")
                : path.Replace("$", parentFolderName);
        }
    }
}
#endif