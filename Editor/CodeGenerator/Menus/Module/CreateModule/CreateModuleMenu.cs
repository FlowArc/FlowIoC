#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";
        private const string CREATE_MODULE_BUTTON = "Create Module";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_MODULE_NAME_TITLE = "Invalid Module Name";
        private const string INVALID_MODULE_NAME_MESSAGE = "Please enter a valid module name.";
        private const string DIRECTORY_CONFIG_ERROR = "Directory config map could not be initialized.";
        private const string FOLDER_STRUCTURE_PREVIEW = "Folder Structure Preview:";
        private const string SCREEN_SETTINGS_LABEL = "Screen Settings:";

        private const string CREATE_SHARED_LABEL = "Create Shared";

        private const string NEW_ACTION = "NewAction";
        private const string ADD_ACTION = "Add Action";
        private const string MODULE_TYPE_LABEL = " Module Type:";
        private const string MODULE_ROLE_LABEL = " Role:";
        private const string NAME_PLACEHOLDER = "Enter module name here...";
        private const string NAME_CONTROL = "flowioc-create-module-name";

        private const string WINDOW_TITLE = "Create Module";
        private const string WINDOW_SUBTITLE = "Folders, assembly, Root and Context";
        private const string HELP_PAGE = "Creating a Module";

        /// <summary>
        /// How tall the two panels are. They stand beside one another, so one height keeps their
        /// bottoms level, whatever the module type is - a screen module keeps the same panels and
        /// puts its own settings under them.
        /// </summary>
        private const float PANEL_HEIGHT = 320f;


        /// <summary>
        /// The name field, and the preview panel beside it: one height, so the row reads as a pair.
        /// </summary>
        private const float NAME_FIELD_HEIGHT = 38f;

        /// <summary>The bar over each panel, and the square button that sits at its right end.</summary>
        private const float PANEL_HEADER_HEIGHT = 33f;

        /// <summary>
        /// How much of the width the left column takes. The right one carries the preview panel,
        /// the folder tree and the toggles, so it is given the larger share.
        /// </summary>
        private const float LEFT_COLUMN_SHARE = 0.45f;

        private const float COLUMNS_SPACING = 8f;
        private const float COLUMNS_MARGIN = 30f;
        private const float COLUMN_MIN_WIDTH = 260f;

        private const string CONFIG_BUTTON_TOOLTIP =
            "Select the folder layout this module type is generated from.";

        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;
        private static readonly Color BUTTON_COLOR_IDLE = Color.cyan;

        private static string _moduleSuffix;
        private static string _moduleName;
        private string _parentModulePath;
        private Dictionary<string, bool> _moduleExpandedState;
        private ModuleRegistry _registry;
        private ModuleSelectionRules _selectionRules;
        private Vector2 _scrollPosition;
        private Vector2 _folderPreviewScrollPosition;
        private Dictionary<ModuleType, DirectoryStructureConfig> _directoryConfigMap;
        private readonly List<FolderEVO> _selectedOptionalFolders = new();
        private ModuleType _selectedModuleType;
        private static GenerationState _generationState;

        /// <summary>
        /// What the module's Root roots, which is what the Root inspector paints it as. It is only
        /// asked of a main module that gets a Root of its own; everywhere else it reads as Core,
        /// the plain Root the generator has always written.
        /// </summary>
        private ModuleRole _selectedModuleRole;

        private readonly ModuleRoleNaming _roleNaming = new();

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private bool _createRoot;
        private bool _createContext;
        private bool _createScene;

        /// <summary>
        /// Whether the module's context is written with [AllowAsSubContext]. A context that has a
        /// Root is left out of Add Sub Context, because adding it to a second Root would build it
        /// twice; a module meant to be hosted on another module's Root says otherwise here. Off by
        /// default, since a module with a Root of its own is the ordinary case.
        /// </summary>
        private bool _allowAsSubContext;

        private string _selectedModuleName = string.Empty;
        private List<string> _actionNames = new();
        private Vector2 _actionScrollPosition;

        private ScreenModuleSettings _screenSettings = new ScreenModuleSettings();

        private void OnEnable()
        {
            _moduleExpandedState = new Dictionary<string, bool>();
            _parentModulePath = string.Empty;
            _moduleName = string.Empty;
            InitializeConfigMap();
            _selectedModuleType = ModuleType.Main;
            _selectedModuleRole = ModuleRole.System;
            _generationState = GenerationState.Idle;
            _createRoot = true;
            _createContext = true;
            _allowAsSubContext = false;
            _actionNames = new List<string>();
            _registry = new ModuleRegistryFactory().FromProject();
            _selectionRules = new ModuleSelectionRules();
            SelectSignalsFolderByDefault();
            SelectSharedFolderByDefault();
        }

        private void OnGUI()
        {
            // The Root's purple, not the scanners' green: those windows report a state, this one
            // writes the Root and Context a module is built around.
            _bar.DrawWindow(
                FlowRole.Root, WINDOW_TITLE, "FlowIoC", WINDOW_SUBTITLE, null, null, HELP_PAGE);

            EditorGUILayout.BeginVertical("box");

            // Both columns are given the same width outright. Left to expand on their own the
            // preview would take everything, because the rows inside it end in a FlexibleSpace.
            // Every row above the panels stands in the same two columns, so the window reads as
            // two lanes rather than as rows that each stretch differently.
            float available = Mathf.Max(position.width - COLUMNS_MARGIN, COLUMN_MIN_WIDTH * 2f);
            float leftWidth = Mathf.Max(available * LEFT_COLUMN_SHARE, COLUMN_MIN_WIDTH);
            float rightWidth = Mathf.Max(available - leftWidth, COLUMN_MIN_WIDTH);

            _moduleSuffix = _selectedModuleType switch
            {
                ModuleType.Main => "",
                ModuleType.Test => "Test",
                ModuleType.Screen => "Screen",
                _ => throw new ArgumentOutOfRangeException()
            };

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(leftWidth));
            DrawNameInputField();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginHorizontal(GUILayout.Width(rightWidth));
            DrawNamePreviewPanel(rightWidth);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            // The type on the left, what the module is made of on the right, over the two columns
            // the panels below stand in. Widening the window widens the gap between them rather
            // than dragging the toggles along behind the dropdown.
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(leftWidth));
            EditorGUILayout.LabelField(MODULE_TYPE_LABEL, EditorStyles.boldLabel, GUILayout.Width(90));

            ModuleType newModuleType = (ModuleType) EditorGUILayout.EnumPopup(_selectedModuleType, GUILayout.ExpandWidth(true));
            if (newModuleType != _selectedModuleType)
            {
                _selectedModuleType = newModuleType;
                OnModuleTypeChanged();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginHorizontal(GUILayout.Width(rightWidth));
            CreateStructureToggles();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // The role names the Root and the Context, so it sits directly under the type that
            // decides whether there is one; what the module publishes and who may host it stand
            // in the column beside it.
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(leftWidth));
            DrawModuleRole();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginHorizontal(GUILayout.Width(rightWidth));
            CreateSharedToggle();
            AllowAsSubContextToggle();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            if (_selectedModuleType == ModuleType.Screen)
                DrawScreenSettings();

            // The two panels answer one question each - what the module will contain, and where it
            // will sit - so they are read together, side by side, and split the width evenly.
            //
            // The preview is drawn whether or not a name has been typed: the layout follows the
            // module type, so it is worth reading before anything else is filled in.
            //
            // Both panels answer to the module name and to nothing else. The parent panel reports a
            // missing parent by turning the rest of the window off, so the preview beside it is
            // handed the name's answer again rather than that report, and the two together decide
            // what is left enabled below.
            bool hasName = GUI.enabled;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
            DisplayParentModuleSelection();
            EditorGUILayout.EndVertical();

            bool hasParent = GUI.enabled;
            GUI.enabled = hasName;

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));
            DisplayFolderStructurePreview();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUI.enabled = hasName && hasParent;

            if (_selectedModuleType == ModuleType.Screen)
            {
                DisplayActionsSection();
            }

            GUILayout.FlexibleSpace();
            DisplayCreateModuleButton();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Unity does not redraw a window that stops being the focused one, and the name field's
        /// hint depends on which window that is, so the two edges are repainted by hand.
        /// </summary>
        private void OnFocus() => Repaint();

        private void OnLostFocus() => Repaint();

        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/gameflexteam/FlowIoC#creating-root--context");
            }
        }

        [DidReloadScripts]
        private static void CodeGenerationCompleted()
        {
            if (_generationState == GenerationState.InProgress)
            {
                _generationState = GenerationState.Completed;
            }
        }
    }
}
#endif