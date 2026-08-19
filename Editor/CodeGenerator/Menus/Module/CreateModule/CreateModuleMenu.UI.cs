#if UNITY_EDITOR
using System.IO;
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

            _excludeFromHierarchy = EditorGUILayout.ToggleLeft("Exclude from Hierarchy", _excludeFromHierarchy, GUILayout.Width(140));

            GUI.backgroundColor = Color.white;
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
                EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(70), GUILayout.MaxHeight(_selectedModuleType == ModuleType.Screen ? 190 : 200));
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

            ModuleHierarchyDrawer.DrawModuleHierarchy(MODULES_PATH, 0, ref _moduleExpandedState, ref _parentModulePath, ref _selectedModuleName, _selectedModuleType);
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
                    _createScene,
                    screenConfigData,
                    _excludeFromHierarchy
                );

                _generationState = GenerationState.Idle;
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif