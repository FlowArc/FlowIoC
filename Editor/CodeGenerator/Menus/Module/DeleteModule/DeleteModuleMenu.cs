#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
            public string ParentInfoFile;
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
                "and parent info file entries.",
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
                        ModuleDeleter.DeleteModule(module.Name, module.Path, module.ParentInfoFile);
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
            _modules = new List<ModuleEntry>();

            string modulesPath = Path.Combine(Application.dataPath, "Modules");
            if (Directory.Exists(modulesPath))
                ScanDirectory(modulesPath);
        }

        private void ScanDirectory(string directory)
        {
            string[] subDirs = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            foreach (string subDir in subDirs)
            {
                string dirName = Path.GetFileName(subDir);

                if (dirName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
                {
                    string moduleType = DetermineModuleType(subDir);
                    string parentInfoFile = FindParentInfoFile(subDir, moduleType);

                    _modules.Add(new ModuleEntry
                    {
                        Name = dirName,
                        Path = subDir,
                        Type = moduleType,
                        ParentInfoFile = parentInfoFile
                    });
                }

                ScanDirectory(subDir);
            }
        }

        private static string DetermineModuleType(string modulePath)
        {
            DirectoryInfo parent = Directory.GetParent(modulePath);
            if (parent == null) return "Main";

            string parentName = parent.Name;

            if (parentName.Equals("Modules", StringComparison.OrdinalIgnoreCase))
                return "Main";
            if (parentName.StartsWith("zTest", StringComparison.OrdinalIgnoreCase))
                return "Test";
            if (parentName.StartsWith("zScreen", StringComparison.OrdinalIgnoreCase))
                return "Screen";
            if (parentName.StartsWith("zSub", StringComparison.OrdinalIgnoreCase))
                return "Sub";

            return "Main";
        }

        private static string FindParentInfoFile(string modulePath, string moduleType)
        {
            DirectoryInfo parent = Directory.GetParent(modulePath);
            if (parent == null) return null;

            string infoFileName = moduleType switch
            {
                "Main" => "_mainmodules_info.txt",
                "Sub" => "_submodules_info.txt",
                "Test" => "_testmodules_info.txt",
                "Screen" => "_screenmodules_info.txt",
                _ => null
            };

            if (infoFileName == null) return null;

            string infoFilePath = Path.Combine(parent.FullName, infoFileName);
            return File.Exists(infoFilePath) ? infoFilePath : null;
        }
    }
}
#endif
