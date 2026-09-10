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

        // "Public" is the word doing the work. A module has two signal holders and only this one
        // crosses a boundary; the other is written whether or not anybody ticks anything. Naming
        // the halves instead - Incoming/Outgoing - would describe the holder's shape rather than
        // what the tick decides, and would be twice as long for it.
        private const string CREATE_SIGNALS_LABEL = "Create Public Signals";

        /// <summary>
        /// How wide an optional-folder toggle is drawn. Wide enough for the longest of them, so the
        /// three in a row line up: a per-label width would leave the second column ragged. No wider,
        /// because the three have to fit the right column at the window's minimum width - the
        /// columns now run to the edge, and what used to spill into the strip kept back for a
        /// scrollbar spills off the window instead.
        /// </summary>
        private const float TOGGLE_WIDTH = 142f;

        private const string NEW_ACTION = "NewAction";
        private const string ADD_ACTION = "Add Action";
        private const string ACTIONS_LABEL = "Actions:";
        private const string MODULE_TYPE_LABEL = " Module Type:";
        private const string MODULE_ROLE_LABEL = " Role:";
        private const string NAME_PLACEHOLDER = "Enter module name here...";
        private const string NAME_CONTROL = "flowioc-create-module-name";

        private const string CARD_LABEL = "Module card";
        private const string CARD_AUDIENCE = "(for AI agents)";
        private const string PURPOSE_CONTROL = "flowioc-create-module-purpose";
        private const string CONCEPTS_CONTROL = "flowioc-create-module-concepts";
        private const string PURPOSE_HINT = "  What this module is for, in one line...";
        private const string CONCEPTS_HINT = "  Words to search for when work belongs here...";

        private const string WINDOW_TITLE = "Create Module";
        private const string WINDOW_SUBTITLE = "Folders, assembly, Root and Context";
        private const string HELP_PAGE = "Create Module";

        /// <summary>
        /// How tall the two panels are. They stand beside one another, so one height keeps their
        /// bottoms level, whatever the module type is.
        /// </summary>
        private const float PANEL_HEIGHT = 320f;

        /// <summary>
        /// The same two panels on a screen module, which draws its settings above them and its
        /// action list below. At the full height the list sat past the window's floor: the form
        /// scrolls, but a section nobody sees is a section nobody fills in, so the panels give the
        /// room up instead. Both of them scroll their own contents already.
        /// </summary>
        private const float SCREEN_PANEL_HEIGHT = 220f;

        /// <summary>
        /// The shortest the action list is drawn. Below this a screen with one action would be a
        /// row and a scrollbar, so the list keeps this much whatever the window does and takes
        /// whatever is left over above it.
        /// </summary>
        private const float ACTION_LIST_MIN_HEIGHT = 60f;

        /// <summary>What the action list leaves under itself when it takes the room that is left.</summary>
        private const float ACTION_LIST_BOTTOM_PADDING = 4f;


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

        private const float COLUMN_MIN_WIDTH = 260f;

        private const string CONFIG_BUTTON_TOOLTIP =
            "Select the folder layout this module type is generated from.";

        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private static string _moduleSuffix;
        private static string _moduleName;

        /// <summary>
        /// The two lines of the module's card, offered here because the author knows what the
        /// module is for at the moment they name it and rarely again afterwards. Both are
        /// optional: an empty one is written as the stub's placeholder and Module Scanner asks
        /// for it later.
        /// </summary>
        private static string _modulePurpose;

        private static string _moduleConcepts;
        private string _parentModulePath;
        private ModulePicker _picker;
        private ModuleRegistry _registry;
        private ModuleSelectionRules _selectionRules;
        private Vector2 _scrollPosition;
        private Vector2 _folderPreviewScrollPosition;

        /// <summary>
        /// The form's scroll view as it was last painted - its width and height, and how tall the
        /// content inside it came to - remembered from the last Repaint because the Layout pass
        /// answers with placeholders. Together they say whether the scrollbar is showing, and the
        /// columns fill the width left of it: nothing is held back for a bar that is not there,
        /// and when one appears the columns give up its width once rather than carrying an empty
        /// strip on every window that never needed to scroll. Measured off the view rather than
        /// off the content, because the content is the columns themselves and a width read from
        /// them would feed back into them, a little wider every frame.
        /// </summary>
        private Rect _view;

        private float _contentHeight;
        private Dictionary<ModuleType, DirectoryStructureConfig> _directoryConfigMap;
        private readonly List<FolderEVO> _selectedOptionalFolders = new();

        // The role's folder is ticked once when the dropdown is first drawn, and again whenever the
        // role changes - not every frame, or unticking Systems by hand would last one repaint.
        private bool _roleFoldersApplied;
        private ModuleType _selectedModuleType;

        /// <summary>
        /// The height the parent panel and the folder preview are both drawn at. It follows the
        /// module type, so the two stay level with one another whichever type is picked.
        /// </summary>
        private float _panelHeight =>
            _selectedModuleType == ModuleType.Screen ? SCREEN_PANEL_HEIGHT : PANEL_HEIGHT;

        private static GenerationState _generationState;

        /// <summary>
        /// What the module's Root roots, which is what the Root inspector paints it as. It is only
        /// asked of a main module that gets a Root of its own; everywhere else it reads as Core,
        /// the plain Root the generator has always written.
        /// </summary>
        private ModuleRole _selectedModuleRole;

        private readonly ModuleRoleNaming _roleNaming = new();

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly FolderPreviewHints _previewHints = new FolderPreviewHints();

        /// <summary>The bar over the screen's actions, with the plus that adds one.</summary>
        private readonly GeneratorListBar _actionsBar = new GeneratorListBar();

        /// <summary>
        /// The column the preview keeps at the row edge for an optional folder's checkbox: the box
        /// and a little air, and no more - on the rows with no box it reads as a gap.
        /// </summary>
        private const float PREVIEW_LEAD_WIDTH = 16f;

        /// <summary>The rows of the folder preview, and the tree that hangs each folder from its parent.</summary>
        private readonly FlowRowPainter _previewRows = new FlowRowPainter();

        private FlowTreePainter _previewTree;

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

        /// <summary>
        /// The height the action list is drawn at, and the height of the form's scroll viewport it
        /// is worked out from. Both are measured on a repaint and read on the layout pass after it,
        /// because GUILayout has already fixed the list's height by the time a repaint could
        /// measure anything.
        /// </summary>
        private float _actionListHeight = ACTION_LIST_MIN_HEIGHT;

        private float _formViewportHeight;

        /// <summary>
        /// The form scrolls, and the Create Module button sits under it rather than in it. A screen
        /// module draws the most: its settings panel and its action list stand between the two
        /// panels and the card, and every action added pushes the rest further down. Without this
        /// the button ended up below the window's bottom edge, out of reach and with nothing on
        /// screen to say it was there.
        /// </summary>
        private Vector2 _windowScrollPosition;

        private ScreenModuleSettings _screenSettings = new ScreenModuleSettings();

        private void OnEnable()
        {
            _parentModulePath = string.Empty;
            _moduleName = string.Empty;
            _modulePurpose = string.Empty;
            _moduleConcepts = string.Empty;
            InitializeConfigMap();
            _selectedModuleType = ModuleType.Main;
            _selectedModuleRole = ModuleRole.System;
            _generationState = GenerationState.Idle;
            _createRoot = true;
            _createContext = true;
            _allowAsSubContext = false;
            _actionNames = new List<string>();
            _registry = new ModuleRegistryFactory().FromProject();
            _picker = new ModulePicker(_registry);
            _previewTree = new FlowTreePainter(_previewRows, PREVIEW_LEAD_WIDTH);

            // Without this the window is sent no MouseMove events at all, and a row of the picker
            // would only light up when something else happened to repaint it.
            wantsMouseMove = true;
            _selectionRules = new ModuleSelectionRules();
            SelectSignalsFolderByDefault();
            ClearSharedFolderByDefault();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            // The Root's purple, not the scanners' green: those windows report a state, this one
            // writes the Root and Context a module is built around.
            _bar.DrawWindow(
                WINDOW_TITLE, "FlowIoC", WINDOW_SUBTITLE, null, null, HELP_PAGE);

            EditorGUILayout.BeginVertical("box");

            // Everything the form asks for scrolls; the button that writes the module does not, so
            // it stays on screen whatever the module type adds above it. The horizontal bar is
            // left out, because the two columns are already fitted to the width.
            _windowScrollPosition = EditorGUILayout.BeginScrollView(
                _windowScrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            // Both columns are given the same width outright. Left to expand on their own the
            // preview would take everything, because the rows inside it end in a FlexibleSpace.
            // Every row above the panels stands in the same two columns, so the window reads as
            // two lanes rather than as rows that each stretch differently.
            bool scrolling = _contentHeight > _view.height;
            float viewWidth = _view.width > 0f ? _view.width : position.width;

            if (scrolling) viewWidth -= GUI.skin.verticalScrollbar.fixedWidth;

            float available = Mathf.Max(viewWidth - COLUMNS_SPACING, COLUMN_MIN_WIDTH * 2f);
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
            CreateSignalsToggle();
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

            // A rect of no height at the foot of the content is where the content ends, and the
            // scroll view's own rect is what it was drawn in.
            Rect foot = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(false));

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                _contentHeight = foot.yMax;
                _view = GUILayoutUtility.GetLastRect();
            }

            // How much room the form was given, which is what the action list inside it stretches
            // to fill. It is the scroll view's own rect, so it is read out here rather than guessed
            // at from the window's height and everything drawn around it.
            if (Event.current.type == EventType.Repaint)
                _formViewportHeight = GUILayoutUtility.GetLastRect().height;

            // The card and the button are the last two things the author touches, so they sit
            // together at the window's floor rather than at the end of the form. Widening the
            // window lengthens the scrolling part above them and they stay where they were.
            DrawModuleCardFields();

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