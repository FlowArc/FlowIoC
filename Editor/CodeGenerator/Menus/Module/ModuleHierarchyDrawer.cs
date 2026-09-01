#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    internal static class ModuleHierarchyDrawer
    {
        private const string MODULE_SUFFIX = "Module";

        public static void DrawModuleHierarchy(
            ModuleRegistry registry,
            string path,
            int indentLevel,
            ref Dictionary<string, bool> moduleExpandedState,
            ref string parentModulePath,
            ref string selectedModuleName,
            Func<ModuleKind, bool> canSelectParent)
        {
            string fullPath = Path.Combine(Application.dataPath, path);
            if (!Directory.Exists(fullPath)) return;

            var pathResolver = new ModuleAssetPathResolver();

            string[] directories = Directory.GetDirectories(fullPath);
            for (var index = 0; index < directories.Length; index++)
            {
                var directory = directories[index];
                if (!registry.TryGetModule(pathResolver.ToAssetPath(directory), out ModuleDescriptorEVO module)) continue;

                string directoryName = Path.GetFileName(directory);
                string moduleTypePostfix = GetModuleTypePostfix(module.Kind);
                string displayName = directoryName + moduleTypePostfix;

                moduleExpandedState.TryAdd(directory, false);

                EditorGUILayout.BeginHorizontal("box");

                GUILayout.Space(indentLevel * 10);
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) {richText = true};
                moduleExpandedState[directory] = EditorGUILayout.Foldout(moduleExpandedState[directory], displayName, true, foldoutStyle);

                bool isSelected = parentModulePath == directory;
                string buttonText = isSelected ? "Selected" : "Select";
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                    hover = {textColor = isSelected ? Color.cyan : Color.yellow}
                };

                if (canSelectParent(module.Kind))
                {
                    if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                    {
                        parentModulePath = directory;
                        selectedModuleName = TrimModuleSuffix(module.Name);
                    }
                }
                else
                {
                    GUILayout.Space(70);
                }

                EditorGUILayout.EndHorizontal();

                if (moduleExpandedState[directory])
                {
                    EditorGUI.indentLevel++;
                    DrawModuleHierarchy(registry, path + "/" + directoryName, indentLevel + 1, ref moduleExpandedState, ref parentModulePath,
                        ref selectedModuleName, canSelectParent);
                    DrawSubModulesRecursively(registry, module, indentLevel + 1, ref moduleExpandedState, ref parentModulePath,
                        ref selectedModuleName, canSelectParent);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static string TrimModuleSuffix(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName) || !moduleName.EndsWith(MODULE_SUFFIX)) return moduleName;
            return moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length).Trim();
        }

        private static string GetModuleTypePostfix(ModuleKind moduleKind)
        {
            switch (moduleKind)
            {
                case ModuleKind.Screen: return " <b>(Screen)</b>";
                case ModuleKind.Test: return " <b>(Test)</b>";
                default: return string.Empty;
            }
        }

        private static void DrawSubModulesRecursively(
            ModuleRegistry registry,
            ModuleDescriptorEVO parentModule,
            int indentLevel,
            ref Dictionary<string, bool> moduleExpandedState,
            ref string parentModulePath,
            ref string selectedModuleName,
            Func<ModuleKind, bool> canSelectParent)
        {
            ModuleKind[] subModuleKinds = {ModuleKind.Sub, ModuleKind.Test, ModuleKind.Screen};

            foreach (ModuleKind kind in subModuleKinds)
            {
                foreach (ModuleDescriptorEVO childModule in registry.ChildrenOf(parentModule, kind))
                {
                    string subDirectory = ToAbsolutePath(registry.PathOf(childModule));
                    string moduleTypePostfix = GetModuleTypePostfix(childModule.Kind);
                    string displayName = childModule.Name + moduleTypePostfix;

                    moduleExpandedState.TryAdd(subDirectory, false);

                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Space(indentLevel * 20 - 10);
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) {richText = true};
                    moduleExpandedState[subDirectory] = EditorGUILayout.Foldout(moduleExpandedState[subDirectory], displayName, true, foldoutStyle);

                    bool isSelected = parentModulePath == subDirectory;
                    string buttonText = isSelected ? "Selected" : "Select";
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                        hover = {textColor = isSelected ? Color.cyan : Color.yellow}
                    };

                    if (canSelectParent(childModule.Kind))
                    {
                        if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                        {
                            parentModulePath = subDirectory;
                            selectedModuleName = TrimModuleSuffix(childModule.Name);
                        }
                    }
                    else
                    {
                        GUILayout.Space(70);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (moduleExpandedState[subDirectory])
                    {
                        DrawSubModulesRecursively(registry, childModule, indentLevel + 1, ref moduleExpandedState, ref parentModulePath,
                            ref selectedModuleName, canSelectParent);
                    }
                }
            }
        }

        /// <summary>
        /// The inverse of ModuleAssetPathResolver.ToAssetPath, kept byte-for-byte identical in separator style to what
        /// Directory.GetDirectories used to hand back (forward slashes through Application.dataPath,
        /// platform separators after). ModuleGenerator still compares parentModulePath against
        /// Path.Combine(Application.dataPath, "Modules") by plain string equality, so drifting the
        /// format here would silently break parent-module detection downstream.
        /// </summary>
        private static string ToAbsolutePath(string assetPath)
        {
            string relative = assetPath.Substring("Assets".Length).Replace('/', Path.DirectorySeparatorChar);
            return Application.dataPath + relative;
        }
    }
}
#endif