#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    internal class DeleteModuleMenu : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<ModuleEntry> _modules;
        private string _searchText = "";

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private struct ModuleEntry
        {
            public string Name;
            public string Path;
            public string Type;
            public string FolderGuid;
        }

        private void OnEnable()
        {
            ScanModules();
        }

        private void OnGUI()
        {
            _bar.DrawWindow(FlowRole.Root, "Delete Module", "FlowIoC",
                "Folder, assembly, settings, index entry and log channel", null, null, "Creating a Module");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Select a module to delete. This will remove the module folder, " +
                "its assembly definition, namespace settings, log type registration, " +
                "and its entry in the module index.",
                MessageType.Warning);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchText = EditorGUILayout.TextField(_searchText);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                ScanModules();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_modules == null || _modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            bool hasSearch = !string.IsNullOrEmpty(_searchText);

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];
                if (hasSearch && module.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // The same row the other module panels draw: the Root's washed violet behind it,
                // and the kind in the colour that kind wears everywhere else.
                GUI.backgroundColor = new ModulePanelTheme().Row;
                EditorGUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField(module.Name, EditorStyles.boldLabel, GUILayout.Width(250));
                DrawKindLabel(module.Type);
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Delete Module",
                            $"Are you sure you want to delete '{module.Name}'?\n\n" +
                            $"Path: {module.Path}\n\n" +
                            "This action cannot be undone!",
                            "Delete", "Cancel"))
                    {
                        IReadOnlyList<string> deleted =
                            ModuleDeleter.DeleteModule(module.Name, module.Path, module.FolderGuid);

                        // The deleter reports rather than announces, so the summary dialog belongs
                        // here, where there is already a user looking at a window.
                        EditorUtility.DisplayDialog(
                            "Module Deleted",
                            $"'{module.Name}' has been deleted.\n\n{string.Join("\n", deleted)}",
                            "OK");

                        ScanModules();
                        GUIUtility.ExitGUI();
                    }
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// What kind of module the row is, in the colour that kind wears in the inspector and in
        /// the module trees. Main and Sub are the ordinary case and say nothing.
        /// </summary>
        private void DrawKindLabel(string kind)
        {
            if (!TryRoleOf(kind, out FlowRole role)) return;

            var content = new GUIContent(kind.ToUpperInvariant());

            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            style.normal.textColor = new FlowPalette().Accent(role, EditorGUIUtility.isProSkin);

            GUILayout.Label(content, style, GUILayout.Width(style.CalcSize(content).x + 10f),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private bool TryRoleOf(string kind, out FlowRole role)
        {
            switch (kind)
            {
                case "Screen":
                    role = FlowRole.Screen;
                    return true;
                case "Test":
                    role = FlowRole.Test;
                    return true;
                default:
                    role = FlowRole.Root;
                    return false;
            }
        }

        private void ScanModules()
        {
            var registry = new ModuleRegistryFactory().FromProject();
            var pathResolver = new ModuleAssetPathResolver();

            _modules = registry.Modules
                .Select(module => ToEntry(module, registry, pathResolver))
                .Where(entry => !string.IsNullOrEmpty(entry.Path))
                .ToList();
        }

        private static ModuleEntry ToEntry(ModuleDescriptorEVO module, ModuleRegistry registry, ModuleAssetPathResolver pathResolver)
        {
            return new ModuleEntry
            {
                Name = module.Name,
                Path = pathResolver.ToAbsolutePath(registry.PathOf(module)),
                Type = module.Kind.ToString(),
                FolderGuid = module.FolderGuid
            };
        }
    }
}
#endif