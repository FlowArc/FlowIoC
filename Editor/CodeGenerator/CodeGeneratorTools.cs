#if UNITY_EDITOR
using FlowIoC.Editor.CodeGenerator.Menus;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using UnityEditor;
using UnityEngine;
using CreateModuleMenu = FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule.CreateModuleMenu;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorTools
    {
        /// <summary>
        /// The floor for the three single-file generators. Each one carries a name, its options, a
        /// module tree and the button that writes the file, and below this the tree is the part
        /// that gives way.
        /// </summary>
        private static readonly Vector2 GENERATOR_WINDOW_SIZE = new Vector2(520, 640);

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
            var window = EditorWindow.GetWindow<CreateViewMenu>("Create View");
            window.minSize = GENERATOR_WINDOW_SIZE;
        }

        [MenuItem("Tools/FlowIoC/Create Model", false, -1297)]
        private static void CreateModelV2()
        {
            var window = EditorWindow.GetWindow<CreateModelMenu>("Create Model");
            window.minSize = GENERATOR_WINDOW_SIZE;
        }

        [MenuItem("Tools/FlowIoC/Create Command", false, -1296)]
        private static void CreateCommandV2()
        {
            var window = EditorWindow.GetWindow<CreateCommandMenu>("Create Command");
            window.minSize = GENERATOR_WINDOW_SIZE;
        }
    }
}
#endif