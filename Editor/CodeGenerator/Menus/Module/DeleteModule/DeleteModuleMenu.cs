#if UNITY_EDITOR
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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Delete Module", EditorStyles.boldLabel);
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

                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField(module.Name, EditorStyles.boldLabel, GUILayout.Width(250));
                EditorGUILayout.LabelField(module.Type, GUILayout.Width(60));

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
                        ModuleDeleter.DeleteModule(module.Name, module.Path, module.FolderGuid);
                        ScanModules();
                        GUIUtility.ExitGUI();
                    }
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanModules()
        {
            var registry = new ModuleRegistry(new ModuleIndexProvider().LoadOrCreate(), new AssetDatabasePaths());
            var pathResolver = new ModuleAssetPathResolver();

            _modules = registry.Modules
                .Select(module => ToEntry(module, registry, pathResolver))
                .Where(entry => !string.IsNullOrEmpty(entry.Path))
                .ToList();
        }

        private static ModuleEntry ToEntry(ModuleDescriptor module, ModuleRegistry registry, ModuleAssetPathResolver pathResolver)
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