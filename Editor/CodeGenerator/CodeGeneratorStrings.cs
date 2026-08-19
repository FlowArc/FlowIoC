#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorStrings
    {
        public const string CONFIG_PATH = "Assets/Editor/FlowIoC/CodeGenerator/CodeGeneratorSettings.asset";
        public const string SCREEN_SERVICE_ROOT_PATH = "Packages/flow-ioc/Assets/Prefabs/ScreenServiceRoot.prefab";
        internal const string SCREEN_MANAGER_PREFAB_PATH = "Packages/flow-ioc/Assets/Resources/Screen/ScreenManager.prefab";
        internal const string CUSTOM_TYPES_FOLDER = "Assets/FlowIoC/Generated";


        internal static readonly string TempViewPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempViews/TempView.cs";
        internal static readonly string TempMediatorPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempViews/TempMediator.cs";

        internal static readonly string TempModelPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempModels/TempModel.cs";
        internal static readonly string TempIModelPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempModels/ITempModel.cs";

        internal static readonly string TempCommandPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempCommands/TempCommand.cs";

        internal static readonly string TempContextPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempRoots/TempContext.cs";
        internal static readonly string TempRootPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempRoots/TempRoot.cs";


        internal static readonly string TempScreenViewPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenView.cs";
        internal static readonly string TempScreenMediatorPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenMediator.cs";

        internal static readonly string TempScreenContextPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenContext.cs";
        internal static readonly string TempScreenRootPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenRoot.cs";

        internal static readonly string TempScreenTestContextPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenTestContext.cs";
        internal static readonly string TempScreenTestRootPath = Application.dataPath.Replace("Assets", "") + "Packages/FlowIoC/Editor/CodeGenerator/TempScreens/TempScreenTestRoot.cs";

        internal static string GetPath(string path, string parentFolderName)
        {
            return string.IsNullOrEmpty(parentFolderName)
                ? path.Replace("$", "Runtime")
                : path.Replace("$", parentFolderName);
        }

    }
}
#endif