#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    internal static class ModuleHierarchyDrawer
    {
        private const string MODULE_SUFFIX = "Module";

        private static readonly ModuleKind[] ChildKinds = {ModuleKind.Sub, ModuleKind.Test, ModuleKind.Screen};
        private static readonly FlowPalette Palette = new FlowPalette();
        private static readonly ModulePanelTheme Theme = new ModulePanelTheme();

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

            foreach (string directory in Directory.GetDirectories(fullPath))
            {
                if (!registry.TryGetModule(pathResolver.ToAssetPath(directory), out ModuleDescriptorEVO module)) continue;

                string directoryName = Path.GetFileName(directory);

                bool expanded = DrawModuleRow(
                    registry, module, directory, directoryName, indentLevel * 10,
                    ref moduleExpandedState, ref parentModulePath, ref selectedModuleName, canSelectParent);

                if (!expanded) continue;

                EditorGUI.indentLevel++;
                DrawModuleHierarchy(registry, path + "/" + directoryName, indentLevel + 1, ref moduleExpandedState,
                    ref parentModulePath, ref selectedModuleName, canSelectParent);
                DrawSubModulesRecursively(registry, module, indentLevel + 1, ref moduleExpandedState, ref parentModulePath,
                    ref selectedModuleName, canSelectParent);
                EditorGUI.indentLevel--;
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
            foreach (ModuleKind kind in ChildKinds)
            {
                foreach (ModuleDescriptorEVO childModule in registry.ChildrenOf(parentModule, kind))
                {
                    string subDirectory = ToAbsolutePath(registry.PathOf(childModule));

                    bool expanded = DrawModuleRow(
                        registry, childModule, subDirectory, childModule.Name, indentLevel * 20 - 10,
                        ref moduleExpandedState, ref parentModulePath, ref selectedModuleName, canSelectParent);

                    if (!expanded) continue;

                    DrawSubModulesRecursively(registry, childModule, indentLevel + 1, ref moduleExpandedState,
                        ref parentModulePath, ref selectedModuleName, canSelectParent);
                }
            }
        }

        /// <summary>
        /// One module: its name, what kind it is, and the button that picks it as the parent.
        /// Answers whether the row is open, which is only ever true for a module that has
        /// something under it - a module with no children is drawn without a foldout arrow, so
        /// the arrow means what it says rather than opening onto nothing.
        /// </summary>
        private static bool DrawModuleRow(
            ModuleRegistry registry,
            ModuleDescriptorEVO module,
            string directory,
            string displayName,
            float indent,
            ref Dictionary<string, bool> moduleExpandedState,
            ref string parentModulePath,
            ref string selectedModuleName,
            Func<ModuleKind, bool> canSelectParent)
        {
            moduleExpandedState.TryAdd(directory, false);

            bool hasChildren = HasChildren(registry, module, directory);

            GUI.backgroundColor = Theme.Row;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

            GUILayout.Space(indent);

            // A nested row is indented by the space above, and EditorGUI.indentLevel would indent
            // the foldout a second time - inside the rect measured for it, so the name ran past
            // its own width and whatever followed was drawn over the tail of it.
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var content = new GUIContent(displayName);

            if (hasChildren)
            {
                var foldoutStyle = new GUIStyle(EditorStyles.foldout) {richText = true};

                // Laid out by hand: the foldout is given exactly the width its name needs, so what
                // follows sits beside the name rather than out at the far edge of the row. The
                // arrow's own indent is added to the text width rather than taken from the style's
                // CalcSize, which measures the text alone and would put the next word on top of it.
                float width = EditorStyles.label.CalcSize(content).x + foldoutStyle.padding.left + 6f;

                Rect rect = GUILayoutUtility.GetRect(
                    width, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(false));

                moduleExpandedState[directory] =
                    EditorGUI.Foldout(rect, moduleExpandedState[directory], displayName, true, foldoutStyle);
            }
            else
            {
                moduleExpandedState[directory] = false;

                var labelStyle = new GUIStyle(EditorStyles.label) {richText = true};

                // The width a foldout spends on its arrow, so the names of the modules that have
                // one and the modules that do not still line up.
                GUILayout.Space(13f);
                GUILayout.Label(content, labelStyle, GUILayout.Width(labelStyle.CalcSize(content).x + 8f),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            DrawKindLabel(module.Kind);

            GUILayout.FlexibleSpace();

            DrawSelectButton(module, directory, ref parentModulePath, ref selectedModuleName, canSelectParent);

            EditorGUI.indentLevel = indentLevel;

            EditorGUILayout.EndHorizontal();

            return moduleExpandedState[directory];
        }

        private static void DrawSelectButton(
            ModuleDescriptorEVO module,
            string directory,
            ref string parentModulePath,
            ref string selectedModuleName,
            Func<ModuleKind, bool> canSelectParent)
        {
            if (!canSelectParent(module.Kind))
            {
                GUILayout.Space(70);
                return;
            }

            bool isSelected = parentModulePath == directory;
            string buttonText = isSelected ? "Selected" : "Select";

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                hover = {textColor = isSelected ? Color.cyan : Color.yellow}
            };

            if (!GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60))) return;

            parentModulePath = directory;
            selectedModuleName = TrimModuleSuffix(module.Name);
        }

        /// <summary>
        /// What kind of module the row is, in the colour its Root wears in the inspector - a
        /// screen module reads as a Screen here for the same reason ScreenRoot does there. A main
        /// or sub module wears none: it is the ordinary case, and a badge on every row would say
        /// nothing.
        /// </summary>
        private static void DrawKindLabel(ModuleKind kind)
        {
            if (!TryRoleOf(kind, out FlowRole role)) return;

            var content = new GUIContent(kind.ToString().ToUpperInvariant());
            GUIStyle style = KindStyle(role);

            GUILayout.Label(content, style, GUILayout.Width(style.CalcSize(content).x + 10f),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        /// <summary>
        /// The role's accent, which is the vivid value on the dark skin and the deep one on the
        /// light skin - the same swap the inspector's own accents make, and the reason the word is
        /// readable on either background.
        /// </summary>
        private static GUIStyle KindStyle(FlowRole role)
        {
            // miniBoldLabel carries a margin of its own, which in a row of mixed heights drops the
            // word below the name beside it. Cleared, and drawn at the row's own line height, the
            // two sit on the same baseline.
            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            style.normal.textColor = Palette.Accent(role, EditorGUIUtility.isProSkin);

            return style;
        }

        private static bool TryRoleOf(ModuleKind kind, out FlowRole role)
        {
            switch (kind)
            {
                case ModuleKind.Screen:
                    role = FlowRole.Screen;
                    return true;
                case ModuleKind.Test:
                    role = FlowRole.Test;
                    return true;
                default:
                    role = FlowRole.Root;
                    return false;
            }
        }

        /// <summary>
        /// Whether anything is nested under the module: a screen, test or sub module the index
        /// knows about, or a module folder sitting directly inside this one.
        /// </summary>
        private static bool HasChildren(ModuleRegistry registry, ModuleDescriptorEVO module, string directory)
        {
            foreach (ModuleKind kind in ChildKinds)
            {
                foreach (ModuleDescriptorEVO unused in registry.ChildrenOf(module, kind))
                    return true;
            }

            return HoldsModuleFolder(registry, directory);
        }

        private static bool HoldsModuleFolder(ModuleRegistry registry, string directory)
        {
            if (!Directory.Exists(directory)) return false;

            var pathResolver = new ModuleAssetPathResolver();

            foreach (string child in Directory.GetDirectories(directory))
            {
                if (registry.TryGetModule(pathResolver.ToAssetPath(child), out ModuleDescriptorEVO _))
                    return true;
            }

            return false;
        }

        private static string TrimModuleSuffix(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName) || !moduleName.EndsWith(MODULE_SUFFIX)) return moduleName;
            return moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length).Trim();
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