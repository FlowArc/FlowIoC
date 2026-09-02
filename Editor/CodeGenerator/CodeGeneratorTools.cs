#if UNITY_EDITOR
using FlowIoC.Editor.CodeGenerator.Detector;
using FlowIoC.Editor.CodeGenerator.Menus;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using FlowIoC.Editor.CodeGenerator.Provider;
using UnityEditor;
using UnityEngine;
using CreateModuleMenu = FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule.CreateModuleMenu;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorTools
    {
        [MenuItem("Tools/FlowIoC/Create Module", false, -1301)]
        private static void CreateModule()
        {
            var window = EditorWindow.GetWindow<CreateModuleMenu>("Create Module");
            window.minSize = new Vector2(800, 800);
        }

        [MenuItem("Tools/FlowIoC/Delete Module", false, -1300)]
        private static void DeleteModule()
        {
            var window = EditorWindow.GetWindow<DeleteModuleMenu>("Delete Module");
            window.minSize = new Vector2(500, 400);
        }

        [MenuItem("Tools/FlowIoC/Create View", false, -1298)]
        private static void CreateViewV2()
        {
            EditorWindow.GetWindow<CreateViewMenu>("Create View");
        }

        [MenuItem("Tools/FlowIoC/Create Model", false, -1297)]
        private static void CreateModelV2()
        {
            EditorWindow.GetWindow<CreateModelMenu>("Create Model");
        }

        [MenuItem("Tools/FlowIoC/Create Command", false, -1296)]
        private static void CreateCommandV2()
        {
            EditorWindow.GetWindow<CreateCommandMenu>("Create Command");
        }

        [MenuItem("Tools/FlowIoC/Module Configuration/Detect & Fix Module Index", false, -1249)]
        private static void FixModuleIndex()
        {
            ModuleAutoDetector.DetectAndRegisterModulesOnStartup();
        }

        [MenuItem("Tools/FlowIoC/Module Configuration/Update Namespace Settings", false, -1247)]
        private static void UpdateNamespaceSettingsMenu()
        {
            NamespaceProvider.UpdateNamespaceSettings();
        }
    }
}
#endif