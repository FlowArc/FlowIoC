#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScan;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Editor
{
    [CustomEditor(typeof(ED_CodeGenerator))]
    public class CodeGeneratorSettingsEditor : UnityEditor.Editor
    {
        private ED_CodeGenerator _settings;
        private FolderEVO.FolderType _selectedNewType = FolderEVO.FolderType.ViewsAndMediators;
        private string _newTypeDefaultName = "NewFolderName";

        private string _newPathKey = "NewKey";
        private string _newPathValue = "NewPath";

        private void OnEnable()
        {
            _settings = (ED_CodeGenerator) target;

            if (_settings.DirectoryStructureConfigMap == null)
            {
                Debug.LogWarning("DirectoryStructureConfigMap is null!");
            }

            if (_settings.DirectoryStructureConfigPaths == null)
            {
                Debug.LogWarning("DirectoryStructureConfigPaths is null!");
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Directory Structure Config Map", EditorStyles.boldLabel);

            if (_settings.DirectoryStructureConfigMap.Count == 0)
            {
                EditorGUILayout.HelpBox("No entries in the DirectoryStructureConfigMap yet.", MessageType.Info);
            }

            DrawExistingEntries();
            EditorGUILayout.Space(10);
            DrawAddEntrySection();

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Directory Structure Config Paths", EditorStyles.boldLabel);

            if (_settings.DirectoryStructureConfigPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("No entries in the DirectoryStructureConfigPaths yet.", MessageType.Info);
            }

            DrawExistingPaths();
            EditorGUILayout.Space(10);
            DrawAddPathEntrySection();
        }

        private void DrawExistingEntries()
        {
            Dictionary<FolderEVO.FolderType, string> map = _settings.DirectoryStructureConfigMap;
            if (map == null) return;

            List<FolderEVO.FolderType> keys = new List<FolderEVO.FolderType>(map.Keys);
            List<FolderEVO.FolderType> keysToRemove = new List<FolderEVO.FolderType>();

            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Update All Locked Folders"))
            {
                if (EditorUtility.DisplayDialog("Folder Update",
                        "This operation will update all locked folders and info files in all modules. Do you want to continue?",
                        "Yes", "No"))
                {
                    EditorUtility.SetDirty(_settings);

                    // ApplyConfiguredFolderNames runs first deliberately. Its fallback locates an
                    // unrecorded module's folder by the name still baked into each
                    // DirectoryStructureConfig's RootFolders - the folder's *current* name, since
                    // nothing has renamed it on disk yet. UpdateDirectoryStructureConfigs resyncs
                    // those RootFolders entries to the *new* configured name; running it first
                    // would make the fallback search for a name nothing on disk carries yet, so an
                    // unrecorded module would never be found and never heal. Reversing the order
                    // costs nothing for an already-recorded module - the GUID path never reads
                    // RootFolders - and still lets this same click resync the folder labels
                    // afterward.
                    _settings.ApplyConfiguredFolderNames();
                    UpdateDirectoryStructureConfigs();
                    AssetDatabase.SaveAssets();
                    new ModuleRepair().FixAll();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            EditorGUILayout.Space(10);

            foreach (FolderEVO.FolderType key in keys)
            {
                string oldValue = map[key];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(160));

                string newValue = EditorGUILayout.TextField(oldValue);
                if (newValue != oldValue)
                {
                    Undo.RecordObject(_settings, "Update Dictionary Entry Value");
                    map[key] = newValue;
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysToRemove.Add(key);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (keysToRemove.Count > 0)
            {
                Undo.RecordObject(_settings, "Remove Dictionary Entries");
                foreach (FolderEVO.FolderType key in keysToRemove)
                {
                    map.Remove(key);
                }

                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawAddEntrySection()
        {
            EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);

            Dictionary<FolderEVO.FolderType, string> map = _settings.DirectoryStructureConfigMap;
            if (map == null) return;

            List<FolderEVO.FolderType> unaddedTypes = System.Enum
                .GetValues(typeof(FolderEVO.FolderType))
                .Cast<FolderEVO.FolderType>()
                .Where(t => t != FolderEVO.FolderType.Folder)
                .Where(t => !map.ContainsKey(t))
                .ToList();

            if (unaddedTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("All FolderType enum values are already in the dictionary.", MessageType.Info);
                return;
            }

            string[] typeNames = unaddedTypes.Select(t => t.ToString()).ToArray();
            int currentIndex = unaddedTypes.IndexOf(_selectedNewType);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("FolderType to Add", currentIndex, typeNames);
            _selectedNewType = unaddedTypes[newIndex];

            _newTypeDefaultName = EditorGUILayout.TextField("Default Name", _newTypeDefaultName);

            if (GUILayout.Button("Add Entry"))
            {
                if (!map.ContainsKey(_selectedNewType))
                {
                    Undo.RecordObject(_settings, "Add Dictionary Entry");
                    map.Add(_selectedNewType, _newTypeDefaultName);
                    EditorUtility.SetDirty(_settings);
                }
                else
                {
                    Debug.LogWarning($"{_selectedNewType} is already in the dictionary!");
                }
            }
        }

        private void DrawExistingPaths()
        {
            Dictionary<string, string> map = _settings.DirectoryStructureConfigPaths;
            if (map == null) return;

            List<string> keys = new List<string>(map.Keys);
            List<string> keysToRemove = new List<string>();

            EditorGUILayout.BeginVertical("box");
            foreach (string key in keys)
            {
                string oldValue = map[key];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(key, GUILayout.Width(160));

                string newValue = EditorGUILayout.TextField(oldValue);
                if (newValue != oldValue)
                {
                    Undo.RecordObject(_settings, "Update Dictionary Entry Value");
                    map[key] = newValue;
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();

                    if (EditorUtility.DisplayDialog("Update Folder Names",
                            "Do you want to update all module folder names with this new value?",
                            "Yes", "No"))
                    {
                        _settings.ApplyConfiguredFolderNames();
                        AssetDatabase.SaveAssets();
                    }
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysToRemove.Add(key);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (keysToRemove.Count > 0)
            {
                Undo.RecordObject(_settings, "Remove Path Dictionary Entries");
                foreach (string key in keysToRemove)
                {
                    map.Remove(key);
                }

                EditorUtility.SetDirty(_settings);
            }
        }

        private void DrawAddPathEntrySection()
        {
            EditorGUILayout.LabelField("Add New Path Entry", EditorStyles.boldLabel);

            Dictionary<string, string> map = _settings.DirectoryStructureConfigPaths;
            if (map == null) return;

            _newPathKey = EditorGUILayout.TextField("Key", _newPathKey);
            _newPathValue = EditorGUILayout.TextField("Path", _newPathValue);

            if (GUILayout.Button("Add Path Entry"))
            {
                if (!map.ContainsKey(_newPathKey))
                {
                    Undo.RecordObject(_settings, "Add Path Dictionary Entry");
                    map.Add(_newPathKey, _newPathValue);
                    EditorUtility.SetDirty(_settings);
                }
                else
                {
                    Debug.LogWarning($"{_newPathKey} is already in the dictionary!");
                }
            }
        }

        private void UpdateDirectoryStructureConfigs()
        {
            foreach (var configPair in _settings.DirectoryStructureConfigPaths)
            {
                var configPath = configPair.Value;
                var configKey = configPair.Key;

                switch (configKey)
                {
                    case "Main":
                        var mainConfig = ED_MainModuleDirectoryStructure.GetOrCreateConfig("Main");
                        UpdateFolderNames(mainConfig.RootFolders);
                        EditorUtility.SetDirty(mainConfig);
                        break;
                    case "Screen":
                        var screenConfig = ED_ScreenModuleDirectoryStructure.GetOrCreateConfig("Screen");
                        UpdateFolderNames(screenConfig.RootFolders);
                        EditorUtility.SetDirty(screenConfig);
                        break;
                    case "Test":
                        var testConfig = ED_TestModuleDirectoryStructure.GetOrCreateConfig("Test");
                        UpdateFolderNames(testConfig.RootFolders);
                        EditorUtility.SetDirty(testConfig);
                        break;
                }
            }
        }

        private void UpdateFolderNames(List<FolderEVO> folders)
        {
            if (folders == null) return;

            foreach (var folder in folders)
            {
                if (_settings.DirectoryStructureConfigMap.TryGetValue(folder.Type, out string newName))
                {
                    folder.FolderName = newName;
                }

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    UpdateFolderNames(folder.SubFolders);
                }
            }
        }
    }
}
#endif