#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
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
        ///
        /// The hint goes as soon as the field has focus. It is drawn on top of the field, so
        /// leaving it up would hide the caret, and clicking would look like nothing happened
        /// until the first character arrived.
        /// </summary>
        private void DrawNameInputField()
        {
            var style = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 0, 0, 0),
                fontSize = 14
            };

            GUI.SetNextControlName(NAME_CONTROL);
            _moduleName = EditorGUILayout.TextField(
                _moduleName, style, GUILayout.ExpandWidth(true), GUILayout.Height(NAME_FIELD_HEIGHT));

            ReleaseNameFieldFocus(GUILayoutUtility.GetLastRect());

            // The window keeps its keyboard control while another window is in front, so the field
            // only counts as focused while this window is the focused one - otherwise clicking
            // away to the Inspector would leave an empty field with no hint and no caret.
            bool isFocused = GUI.GetNameOfFocusedControl() == NAME_CONTROL && focusedWindow == this;

            if (isFocused || !string.IsNullOrEmpty(_moduleName)) return;

            var hintStyle = new GUIStyle(style)
            {
                normal = {textColor = Color.gray, background = null}
            };

            GUI.Label(GUILayoutUtility.GetLastRect(), NAME_PLACEHOLDER, hintStyle);
        }

        /// <summary>
        /// Lets go of the name field when the click lands somewhere else, or on Escape. IMGUI
        /// keeps the keyboard on a text field until another control takes it, so clicking the
        /// window's background left the field focused, the caret blinking and the hint still
        /// withheld from an empty field.
        ///
        /// The mouse event is not consumed - whatever was clicked still gets it, and takes the
        /// keyboard itself if it wants it.
        /// </summary>
        private void ReleaseNameFieldFocus(Rect fieldRect)
        {
            if (GUI.GetNameOfFocusedControl() != NAME_CONTROL) return;

            Event current = Event.current;

            bool clickedElsewhere = current.type == EventType.MouseDown && !fieldRect.Contains(current.mousePosition);
            bool cancelled = current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape;

            if (!clickedElsewhere && !cancelled) return;

            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;

            if (cancelled) current.Use();

            Repaint();
        }

        /// <summary>
        /// What the name typed beside it is about to produce. The left half names the folder and
        /// the Context; the right half draws the Root as the inspector will draw it, in the colour
        /// its role gives it, so the choice of role is read as a picture rather than as a word.
        ///
        /// While no name is typed the panel is the error that says so, and the rest of the window
        /// stays off - the same contract the parent-module panel keeps.
        /// </summary>
        private void DrawNamePreviewPanel(float width)
        {
            // Margins cleared: three stacked labels each keeping the label style's own vertical
            // margin is what made the panel tall, and the lines belong together anyway.
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            if (string.IsNullOrEmpty(_moduleName))
            {
                GUI.backgroundColor = Color.red;

                var box = new GUIStyle(EditorStyles.helpBox) {padding = new RectOffset(4, 4, 0, 0)};

                EditorGUILayout.BeginHorizontal(box, GUILayout.Height(NAME_FIELD_HEIGHT));

                // Both fill the box's full height and centre themselves in it. The box gives up its
                // own vertical padding for that: with padding left in, content the height of the
                // box pushes the box past the name field beside it, and content short enough to
                // fit rides at the top of it.
                // A little top margin under the icon: an image in a style of its own is drawn from
                // the top of its rect whatever the alignment says, so it needs pushing onto the
                // line the words sit on.
                var icon = new GUIStyle(GUIStyle.none)
                {
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 4, 2, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };

                GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), icon,
                    GUILayout.Width(30), GUILayout.Height(NAME_FIELD_HEIGHT));
                EditorGUILayout.LabelField("<size=12>Please, enter <b>module name</b>!</size>", style,
                    GUILayout.Height(NAME_FIELD_HEIGHT));
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;
                GUI.enabled = false;
                return;
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(NAME_FIELD_HEIGHT));

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(18), GUILayout.Height(13));
            EditorGUILayout.LabelField("<size=10><color=#ffdd00ff>Preview</color></size>", style, GUILayout.Height(13));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"<size=10>Folder Name:</size> <size=12><color=#ffdd00ff><u>{_moduleName}{_moduleSuffix}Module</u></color></size>",
                style, GUILayout.Height(15));
            EditorGUILayout.EndVertical();

            DrawRootHeaderPreview(width * 0.5f);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The Root the module will get, drawn the way its own inspector will draw it: the role's
        /// deep fill, the accent stripe down its left, and the strip underneath naming the assembly
        /// and the role. It is only shown for a module that gets a Root and a role to colour it.
        /// </summary>
        private void DrawRootHeaderPreview(float width)
        {
            if (_selectedModuleType != ModuleType.Main || !_createRoot) return;

            FlowRole role = PreviewRole();
            var palette = new FlowPalette();

            Color deep = palette.Deep(role);
            Color accent = palette.Accent(role, EditorGUIUtility.isProSkin);

            // No space above it: the bar and its strip fill the panel's height exactly, and a gap
            // here is what pushed the panel past the name field it stands beside.
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            Rect bar = GUILayoutUtility.GetRect(width, 20f);
            Rect strip = GUILayoutUtility.GetRect(width, 10f);

            EditorGUI.DrawRect(bar, deep);
            EditorGUI.DrawRect(strip, palette.Strip(deep));
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, 3f, bar.height + strip.height), accent);

            var title = new GUIStyle(EditorStyles.miniBoldLabel) {alignment = TextAnchor.MiddleLeft};
            title.normal.textColor = palette.Title;

            var label = new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleLeft, fontSize = 9};
            label.normal.textColor = accent;

            GUI.Label(new Rect(bar.x + 9f, bar.y, bar.width - 12f, bar.height),
                Spaced(_roleNaming.RootName(_moduleName, _effectiveRole)).ToUpperInvariant(), title);

            GUI.Label(new Rect(strip.x + 9f, strip.y, strip.width - 12f, strip.height),
                $"Modules.{_moduleName} · {RoleLabel(role)}".ToUpperInvariant(), label);

            EditorGUILayout.EndVertical();
        }

        /// <summary>The role the generated Root will resolve to from its own name.</summary>
        private FlowRole PreviewRole()
        {
            switch (_effectiveRole)
            {
                case ModuleRole.System: return FlowRole.System;
                case ModuleRole.Service: return FlowRole.Service;
                default: return FlowRole.Root;
            }
        }

        /// <summary>
        /// What the strip calls it. A Root wearing another role's colour still says that it is a
        /// Root, the way the inspector's own bar does.
        /// </summary>
        private string RoleLabel(FlowRole role) => role == FlowRole.Root ? "Root" : $"{role} · Root";

        /// <summary>The class name split at its capitals, so PlayerSystemRoot reads as three words.</summary>
        private string Spaced(string name)
        {
            var spaced = new System.Text.StringBuilder(name.Length + 4);

            for (int ii = 0; ii < name.Length; ii++)
            {
                if (ii > 0 && char.IsUpper(name[ii]) && !char.IsUpper(name[ii - 1]))
                    spaced.Append(' ');

                spaced.Append(name[ii]);
            }

            return spaced.ToString();
        }

        /// <summary>
        /// What the module is made of: the Context, the Root that builds it, and a scene to put
        /// that Root in. They sit on the module type's own row, because the type is the other half
        /// of the same answer, and each one gates the next - no Context, no Root; no Root, no scene.
        ///
        /// A gated toggle is shown off and disabled rather than hidden. The three together are what
        /// the module will be written with, and a row that loses an entry as it is ticked reads as
        /// though the choice went somewhere.
        /// </summary>
        private void CreateStructureToggles()
        {
            GUI.backgroundColor = new Color(.6f, .7f, 1f);

            bool isTest = _selectedModuleType == ModuleType.Test;
            bool isScreen = _selectedModuleType == ModuleType.Screen;

            // A test module is its scene, and the Root and Context that run it, so it is handed all
            // three rather than asked.
            if (isTest)
            {
                _createContext = true;
                _createRoot = true;
                _createScene = true;
            }

            using (new EditorGUI.DisabledScope(isTest))
            {
                _createContext = EditorGUILayout.ToggleLeft("Create Context", _createContext, GUILayout.Width(120));
            }

            if (!_createContext) _createRoot = false;

            using (new EditorGUI.DisabledScope(isTest || !_createContext))
            {
                _createRoot = EditorGUILayout.ToggleLeft("Create Root", _createRoot, GUILayout.Width(105));
            }

            if (!_createRoot) _createScene = false;

            // A screen module brings the scene its test module runs in, so the answer is yes and
            // the toggle only says so.
            if (isScreen && _createRoot) _createScene = true;

            using (new EditorGUI.DisabledScope(isTest || isScreen || !_createRoot))
            {
                _createScene = EditorGUILayout.ToggleLeft("Create Scene", _createScene, GUILayout.Width(115));
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

            EditorGUILayout.LabelField(MODULE_ROLE_LABEL, EditorStyles.boldLabel, GUILayout.Width(90));
            _selectedModuleRole = (ModuleRole) EditorGUILayout.EnumPopup(_selectedModuleRole, GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// The role the generator is handed. A module that is not offered the dropdown is written
        /// the way it always was, whatever the dropdown happens to be showing, and the choice is
        /// kept rather than reset so switching Create Root off and on again does not lose it.
        /// </summary>
        private ModuleRole _effectiveRole =>
            _selectedModuleType == ModuleType.Main && _createRoot ? _selectedModuleRole : ModuleRole.Core;

        /// <summary>
        /// The Shared assembly a module publishes its data and its signal holder through. The main
        /// and screen layouts carry the folder and the test layout does not, so the toggle draws
        /// itself where it belongs without having to name the types. It starts ticked, because a
        /// module created without Shared has nowhere to put its public surface.
        ///
        /// A screen module is not asked and not told either: its signals are the only way into the
        /// screen and they live in Shared, so the folder is simply taken and the row it would have
        /// occupied goes back to the panels below.
        /// </summary>
        private void CreateSharedToggle() =>
            OptionalFolderToggle(
                FolderEVO.FolderType.Shared, CREATE_SHARED_LABEL,
                withheldFrom: null, requiredFor: new[] {ModuleType.Screen}, drawWhenRequired: false);

        /// <summary>
        /// Whether the module gets signal holders written. There is no toggle for it any more:
        /// the public holder lives in Shared and the internal one in the Runtime Signals folder,
        /// so the answer is simply whether either folder is going to exist - which the reader says
        /// by ticking them in the folder structure.
        /// </summary>
        private bool SignalsWanted() =>
            FolderWillExist(FolderEVO.FolderType.Signals) || FolderWillExist(FolderEVO.FolderType.Shared);

        private bool FolderWillExist(FolderEVO.FolderType folderType)
        {
            FolderEVO folder = FindFolderInConfig(folderType);

            return folder != null && (folder.IsMandatory || _selectedOptionalFolders.Contains(folder));
        }

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
            ModuleType[] withheldFrom = null,
            ModuleType[] requiredFor = null,
            bool drawWhenRequired = true)
        {
            FolderEVO folder = FindFolderInConfig(folderType);

            OptionalFolderToggleState state =
                new OptionalFolderToggleRule().For(folder, _selectedModuleType, withheldFrom, requiredFor);

            if (state == OptionalFolderToggleState.Hidden)
                return false;

            // A folder the layout calls optional is only created when it is in the selection, so a
            // locked-on toggle has to put it there rather than only say so.
            if (state == OptionalFolderToggleState.ForcedOn && folder.IsOptional && !_selectedOptionalFolders.Contains(folder))
                _selectedOptionalFolders.Add(folder);

            // A folder this module type must have and cannot decline is worth no row of its own:
            // it is taken, and the row goes to what the reader can actually change.
            if (state == OptionalFolderToggleState.ForcedOn && !drawWhenRequired)
                return true;

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

            bool wasEnabled = GUI.enabled;
            bool hasParent = !string.IsNullOrEmpty(_parentModulePath);

            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            // The panel's own bar carries the ask: red and saying so while no parent is picked,
            // and the panel's purple heading once one is. A second bar under the list said the same
            // thing in a place the eye had already left.
            GUI.backgroundColor = hasParent ? new ModulePanelTheme().Header : Color.red;

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(PANEL_HEADER_HEIGHT));
            GUILayout.Label(EditorGUIUtility.IconContent(hasParent ? "console.infoicon" : "console.erroricon"),
                GUILayout.Width(35), GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.LabelField(
                hasParent ? PARENT_MODULE_LABEL : "<size=12>Please, select <b>parent module</b>!</size>",
                style, GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;

            float height = PANEL_HEIGHT;

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition, GUILayout.MinHeight(height), GUILayout.MaxHeight(height));
            EditorGUILayout.BeginVertical();
            DrawModulesHierarchy();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            // What the bar reports, said again to the rest of the window: no parent, nothing below
            // this panel is worth pressing.
            GUI.enabled = wasEnabled && hasParent;
        }

        private void DrawModulesHierarchy()
        {
            GUI.backgroundColor = new ModulePanelTheme().Row;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

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
                    SignalsWanted(),
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