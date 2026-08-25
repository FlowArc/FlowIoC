#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        private void DrawNameInputField()
        {
            var style = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 0, 0, 0),
                fontSize = 14
            };
            if (string.IsNullOrEmpty(_moduleName))
            {
                style.normal.textColor = Color.gray;
                _moduleName = EditorGUILayout.TextField("Enter module name here...", style, GUILayout.MaxWidth(400), GUILayout.Height(38));
                if (_moduleName == "Enter module name here...") _moduleName = "";
            }
            else
            {
                _moduleName = EditorGUILayout.TextField(_moduleName, style, GUILayout.MaxWidth(400), GUILayout.Height(38));
            }
        }

        private void CustomCheck(string errorMessage, string checkString, string logMessage)
        {
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            int height = 33;
            bool isInvalid = string.IsNullOrEmpty(checkString);
            if (isInvalid)
            {
                GUI.backgroundColor = Color.red;
                EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(height));
                GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), GUILayout.Width(35), GUILayout.Height(height));
                EditorGUILayout.LabelField(errorMessage, style, GUILayout.Height(height));
                EditorGUILayout.EndHorizontal();
            }
            else if (!string.IsNullOrEmpty(logMessage))
            {
                GUI.backgroundColor = Color.white;
                EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(height));
                GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(35), GUILayout.Height(height));
                EditorGUILayout.LabelField(logMessage, style, GUILayout.Height(height));
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = !isInvalid;
            GUI.backgroundColor = Color.white;
        }

        private void CreateToggles()
        {
            GUI.backgroundColor = new Color(.6f, .7f, 1f);

            using (new EditorGUI.DisabledScope(_selectedModuleType == ModuleType.Test))
            {
                if (_selectedModuleType == ModuleType.Test)
                {
                    _createContext = true;
                    _createRoot = true;
                    _createScene = true;
                }

                _createContext = EditorGUILayout.ToggleLeft("Create Context", _createContext, GUILayout.Width(125));
                if (_createContext)
                {
                    _createRoot = EditorGUILayout.ToggleLeft("Create Root", _createRoot, GUILayout.Width(125));
                }
                else
                {
                    _createRoot = false;
                    _createScene = false;
                }
            }

            if (_createRoot)
            {
                _createScene = _selectedModuleType switch
                {
                    ModuleType.Main or ModuleType.Test => EditorGUILayout.ToggleLeft("Create Scene", _createScene, GUILayout.Width(125)),
                    ModuleType.Screen => true,
                    _ => _createScene
                };
            }

            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// The signals toggle sits on its own row and is the same state as the Signals entry in
        /// the folder preview, so ticking either one cannot leave a module holding an empty
        /// Signals folder. Where the config marks the folder mandatory - the Screen config does,
        /// because a screen module generates no Context and its holder is the only way in - the
        /// toggle is shown on and disabled rather than hidden.
        /// </summary>
        private void CreateSignalsToggle()
        {
            FolderConfig signalsFolder = FindSignalsFolder();

            // A test module wires other modules' signals rather than owning a public surface of
            // its own, so it is the one module type never offered a holder.
            if (signalsFolder == null || _selectedModuleType == ModuleType.Test)
            {
                _createSignals = false;
                return;
            }

            GUI.backgroundColor = new Color(.6f, .7f, 1f);

            if (!signalsFolder.IsOptional)
            {
                _createSignals = true;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ToggleLeft(CREATE_SIGNALS_LABEL, true, GUILayout.Width(125));
                }
            }
            else
            {
                bool wasSelected = _selectedOptionalFolders.Contains(signalsFolder);
                bool nowSelected = EditorGUILayout.ToggleLeft(CREATE_SIGNALS_LABEL, wasSelected, GUILayout.Width(125));

                if (nowSelected && !wasSelected)
                {
                    _selectedOptionalFolders.Add(signalsFolder);
                }
                else if (!nowSelected && wasSelected)
                {
                    _selectedOptionalFolders.Remove(signalsFolder);
                }

                _createSignals = nowSelected;
            }

            GUI.backgroundColor = Color.white;
        }

        private void SelectSignalsFolderByDefault()
        {
            FolderConfig signalsFolder = FindSignalsFolder();

            if (signalsFolder == null || !signalsFolder.IsOptional) return;
            if (_selectedModuleType == ModuleType.Test) return;
            if (_selectedOptionalFolders.Contains(signalsFolder)) return;

            _selectedOptionalFolders.Add(signalsFolder);
        }

        private FolderConfig FindSignalsFolder()
        {
            if (_directoryConfigMap == null) return null;

            return _directoryConfigMap.TryGetValue(_selectedModuleType, out DirectoryStructureConfig config) && config != null
                ? FindFolderByType(config.RootFolders, FolderConfig.FolderType.Signals)
                : null;
        }

        private FolderConfig FindFolderByType(List<FolderConfig> folders, FolderConfig.FolderType folderType)
        {
            if (folders == null) return null;

            foreach (FolderConfig folder in folders)
            {
                if (folder.Type == folderType) return folder;

                FolderConfig found = FindFolderByType(folder.SubFolders, folderType);
                if (found != null) return found;
            }

            return null;
        }

        private void DisplayParentModuleSelection()
        {
            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(.6f, .4f, 1f);
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(33));
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(35), GUILayout.Height(33));
            EditorGUILayout.LabelField(PARENT_MODULE_LABEL, style, GUILayout.Height(33));
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            _scrollPosition =
                EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(70),
                    GUILayout.MaxHeight(_selectedModuleType == ModuleType.Screen ? 190 : 200));
            EditorGUILayout.BeginVertical();
            DrawModulesHierarchy();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (GUI.enabled) CustomCheck("<size=12>Please, select <b>parent module</b>!</size>", _parentModulePath, "");
        }

        private void DrawModulesHierarchy()
        {
            EditorGUILayout.BeginHorizontal("box");

            GUILayout.Space(10);
            bool isSelected = _parentModulePath == Path.Combine(Application.dataPath, MODULES_PATH);
            string buttonText = isSelected ? "Selected" : "Select";
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                hover = {textColor = isSelected ? Color.cyan : Color.yellow}
            };

            EditorGUILayout.LabelField("Modules", EditorStyles.label);

            if (_selectedModuleType == ModuleType.Main)
            {
                if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                {
                    _parentModulePath = Path.Combine(Application.dataPath, MODULES_PATH);
                    _selectedModuleName = string.Empty;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Select", GUILayout.Width(60));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();

            ModuleHierarchyDrawer.DrawModuleHierarchy(_registry, MODULES_PATH, 0, ref _moduleExpandedState, ref _parentModulePath,
                ref _selectedModuleName, parent => _selectionRules.CanHost(ToModuleKind(_selectedModuleType), parent));
        }

        /// <summary>
        /// CreateModuleMenu's own ModuleType has no Sub value; Main/Test/Screen map straight
        /// across to their ModuleKind counterparts, and a nested Sub module is never a value
        /// this dropdown offers, so it never needs to appear on this side of the conversion.
        /// </summary>
        private ModuleKind ToModuleKind(ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Test: return ModuleKind.Test;
                case ModuleType.Screen: return ModuleKind.Screen;
                default: return ModuleKind.Main;
            }
        }

        private void DisplayCreateModuleButton()
        {
            GUI.backgroundColor = _generationState == GenerationState.InProgress ? BUTTON_COLOR_IN_PROGRESS : BUTTON_COLOR_IDLE;
            EditorGUI.BeginDisabledGroup(_generationState == GenerationState.InProgress);
            if (GUILayout.Button(CREATE_MODULE_BUTTON, GUILayout.Height(40)))
            {
                if (string.IsNullOrEmpty(_moduleName))
                {
                    EditorUtility.DisplayDialog(INVALID_MODULE_NAME_TITLE, INVALID_MODULE_NAME_MESSAGE, "OK");
                    return;
                }

                _generationState = GenerationState.InProgress;

                ScreenConfigData screenConfigData = null;
                if (_selectedModuleType == ModuleType.Screen && _screenConfigPreview != null)
                {
                    screenConfigData = new ScreenConfigData
                    {
                        DefaultLayer = _screenConfigPreview.DefaultLayer,
                        LoadType = _screenConfigPreview.LoadType,
                        ResourcePath = _screenConfigPreview.ResourcePath,
                        AddressableKey = _screenConfigPreview.AddressableKey,
                        HasOpenAnimation = _screenConfigPreview.HasShowAnimation,
                        HasCloseAnimation = _screenConfigPreview.HasHideAnimation,
                        Tag = _screenConfigPreview.Tag
                    };
                }

                ModuleGeneration.ModuleGenerator.CreateModuleStructure(
                    _moduleName + _moduleSuffix,
                    _parentModulePath,
                    _selectedModuleType,
                    _selectedOptionalFolders,
                    _directoryConfigMap,
                    _actionNames,
                    _createRoot,
                    _createContext,
                    _createSignals,
                    _createScene,
                    screenConfigData
                );

                _generationState = GenerationState.Idle;
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif