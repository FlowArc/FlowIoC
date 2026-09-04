#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        /// <summary>
        /// The module name, with the hint drawn over an empty field rather than typed into it.
        /// The hint used to be the field's own text, so clicking selected those words and typing
        /// replaced them - and a name that happened to match them was read as no name at all.
        /// A label paints over the field instead: it takes no input, so the field underneath is
        /// empty and behaves like one.
        /// </summary>
        private void DrawNameInputField()
        {
            var style = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 0, 0, 0),
                fontSize = 14
            };

            _moduleName = EditorGUILayout.TextField(
                _moduleName, style, GUILayout.MaxWidth(400), GUILayout.Height(38));

            if (!string.IsNullOrEmpty(_moduleName)) return;

            var hintStyle = new GUIStyle(style)
            {
                normal = {textColor = Color.gray, background = null}
            };

            GUI.Label(GUILayoutUtility.GetLastRect(), NAME_PLACEHOLDER, hintStyle);
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

            AllowAsSubContextToggle();

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
        /// Whether the context is written with [AllowAsSubContext]. It is only asked of a module
        /// that gets a Root of its own, because a context without one is listed in Add Sub Context
        /// anyway. A screen module's context is a sub-context already and a test module's is never
        /// offered, so neither is asked.
        /// </summary>
        private void AllowAsSubContextToggle()
        {
            if (_selectedModuleType != ModuleType.Main || !_createRoot)
            {
                _allowAsSubContext = false;
                return;
            }

            _allowAsSubContext = EditorGUILayout.ToggleLeft(
                "Allow As Sub Context", _allowAsSubContext, GUILayout.Width(160));
        }

        /// <summary>
        /// What the module's Root roots. The Root inspector reads its colour off the Root's own
        /// name, so picking a role here is what makes a Service module's Root draw as a Service
        /// and a System module's as a System; the module, its folder and its assembly keep the
        /// name that says what they do.
        ///
        /// Only a main module that gets a Root is asked. A screen or test module's Root is neither,
        /// and a module that generates no Root has nothing to colour.
        /// </summary>
        private void DrawModuleRole()
        {
            if (_selectedModuleType != ModuleType.Main || !_createRoot)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(MODULE_ROLE_LABEL, EditorStyles.boldLabel, GUILayout.Width(90));
            _selectedModuleRole = (ModuleRole) EditorGUILayout.EnumPopup(_selectedModuleRole, GUILayout.Width(310));

            if (!string.IsNullOrEmpty(_moduleName))
            {
                EditorGUILayout.LabelField(
                    $"{_roleNaming.RootName(_moduleName, _selectedModuleRole)} · " +
                    $"{_roleNaming.ContextName(_moduleName, _selectedModuleRole)}",
                    GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The role the generator is handed. A module that is not offered the dropdown is written
        /// the way it always was, whatever the dropdown happens to be showing, and the choice is
        /// kept rather than reset so switching Create Root off and on again does not lose it.
        /// </summary>
        private ModuleRole _effectiveRole =>
            _selectedModuleType == ModuleType.Main && _createRoot ? _selectedModuleRole : ModuleRole.Core;

        /// <summary>
        /// The signals holder. A test module wires other modules' signals rather than owning a
        /// public surface of its own, so it is the one module type never offered one.
        /// </summary>
        private void CreateSignalsToggle() =>
            _createSignals = OptionalFolderToggle(
                FolderEVO.FolderType.Signals, CREATE_SIGNALS_LABEL, ModuleType.Test);

        /// <summary>
        /// The Shared assembly a module publishes its data and its signal holder through. The main
        /// and screen layouts carry the folder and the test layout does not, so the toggle draws
        /// itself where it belongs without having to name the types. It starts ticked, because a
        /// module created without Shared has nowhere to put its public surface.
        /// </summary>
        private void CreateSharedToggle() =>
            OptionalFolderToggle(FolderEVO.FolderType.Shared, CREATE_SHARED_LABEL);

        /// <summary>
        /// Draws the toggle for one of the layout's optional folders and answers whether the
        /// folder is to be created. It is the same state as that folder's entry in the preview
        /// below, so ticking either one cannot leave a module holding a folder the other says it
        /// should not have.
        ///
        /// A folder the layout marks mandatory - the Screen layout does that to Signals, because a
        /// screen module generates no Context and its holder is the only way in - is shown ticked
        /// and disabled rather than hidden, so the reader can see what they are getting.
        /// </summary>
        private bool OptionalFolderToggle(
            FolderEVO.FolderType folderType,
            string label,
            params ModuleType[] withheldFrom)
        {
            FolderEVO folder = FindFolderInConfig(folderType);

            OptionalFolderToggleState state =
                new OptionalFolderToggleRule().For(folder, _selectedModuleType, withheldFrom);

            if (state == OptionalFolderToggleState.Hidden)
                return false;

            GUI.backgroundColor = new Color(.6f, .7f, 1f);

            bool isSelected;

            if (state == OptionalFolderToggleState.ForcedOn)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ToggleLeft(label, true, GUILayout.Width(125));
                }

                isSelected = true;
            }
            else
            {
                bool wasSelected = _selectedOptionalFolders.Contains(folder);
                bool nowSelected = EditorGUILayout.ToggleLeft(label, wasSelected, GUILayout.Width(125));

                if (nowSelected && !wasSelected)
                {
                    _selectedOptionalFolders.Add(folder);
                }
                else if (!nowSelected && wasSelected)
                {
                    _selectedOptionalFolders.Remove(folder);
                }

                isSelected = nowSelected;
            }

            GUI.backgroundColor = Color.white;

            return isSelected;
        }

        private void SelectSignalsFolderByDefault()
        {
            FolderEVO signalsFolder = FindSignalsFolder();

            if (signalsFolder == null || !signalsFolder.IsOptional) return;
            if (_selectedModuleType == ModuleType.Test) return;
            if (_selectedOptionalFolders.Contains(signalsFolder)) return;

            _selectedOptionalFolders.Add(signalsFolder);
        }

        /// <summary>
        /// Shared starts ticked. It stays a toggle - a module that publishes nothing can still be
        /// left without one - but the public signal holder lives in that assembly now, so a module
        /// created without Shared has nowhere to put the surface every other module talks to it
        /// through.
        /// </summary>
        private void SelectSharedFolderByDefault()
        {
            FolderEVO sharedFolder = FindFolderInConfig(FolderEVO.FolderType.Shared);

            if (sharedFolder == null || !sharedFolder.IsOptional) return;
            if (_selectedModuleType == ModuleType.Test) return;
            if (_selectedOptionalFolders.Contains(sharedFolder)) return;

            _selectedOptionalFolders.Add(sharedFolder);
        }

        private FolderEVO FindSignalsFolder() => FindFolderInConfig(FolderEVO.FolderType.Signals);

        /// <summary>
        /// The selected module type's layout entry for <paramref name="folderType"/>, or null when
        /// that layout has none - which is how a toggle knows to stay out of the way.
        /// </summary>
        private FolderEVO FindFolderInConfig(FolderEVO.FolderType folderType)
        {
            if (_directoryConfigMap == null) return null;

            return _directoryConfigMap.TryGetValue(_selectedModuleType, out DirectoryStructureConfig config) && config != null
                ? FindFolderByType(config.RootFolders, folderType)
                : null;
        }

        private FolderEVO FindFolderByType(List<FolderEVO> folders, FolderEVO.FolderType folderType)
        {
            if (folders == null) return null;

            foreach (FolderEVO folder in folders)
            {
                if (folder.Type == folderType) return folder;

                FolderEVO found = FindFolderByType(folder.SubFolders, folderType);
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

                ScreenModuleSettings screenSettings = null;
                if (_selectedModuleType == ModuleType.Screen)
                {
                    _screenSettings.AddressableKey = _moduleName + _moduleSuffix;
                    screenSettings = _screenSettings;
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
                    _allowAsSubContext,
                    _effectiveRole,
                    screenSettings
                );

                _generationState = GenerationState.Idle;
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif