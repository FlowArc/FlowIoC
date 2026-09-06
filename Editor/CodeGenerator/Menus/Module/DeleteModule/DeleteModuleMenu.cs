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
            _bar.DrawWindow("Delete Module", "FlowIoC",
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
                            LeftBehind(module) +
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

        /// <summary>
        /// The scenes and prefabs that point into this module, as a block for the confirmation
        /// dialog, or nothing at all when none do.
        ///
        /// A Root holds its sub-contexts by script reference, so a scene listing one of this
        /// module's contexts is a dependent asset the engine tracks and this can name it. Delete
        /// Module still deletes: what it does not do is open the scene and edit it, because that is
        /// the reader's call. Being told which files to look at afterwards is the difference between
        /// a scene that silently stops building a sub-context and one somebody knows about.
        /// </summary>
        private string LeftBehind(ModuleEntry module)
        {
            IReadOnlyList<string> referenced = new ModuleAssetReferences()
                .Find(new ModuleAssetPathResolver().ToAssetPath(module.Path));

            if (referenced.Count == 0) return string.Empty;

            // A dialog is not a report. Past a handful the list stops being readable, and the console
            // line the deleter writes carries the rest.
            const int shown = 6;

            string list = string.Join("\n", referenced.Take(shown));

            if (referenced.Count > shown)
                list += $"\n...and {referenced.Count - shown} more";

            return "These still point into it and are left as they are:\n" + list + "\n\n";
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