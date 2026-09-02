#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
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

        private const string CREATE_SIGNALS_LABEL = "Create Signals";
        private const string CREATE_SHARED_LABEL = "Create Shared";

        private const string NEW_ACTION = "NewAction";
        private const string ADD_ACTION = "Add Action";
        private const string MODULE_TYPE_LABEL = " Module Type:";
        private const string CONFIG_TYPE_LABEL = "Config:";
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

        private bool _createRoot;
        private bool _createContext;
        private bool _createSignals;
        private bool _createScene;
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
            _generationState = GenerationState.Idle;
            _createRoot = true;
            _createContext = true;
            _createSignals = true;
            _actionNames = new List<string>();
            _registry = new ModuleRegistryFactory().FromProject();
            _selectionRules = new ModuleSelectionRules();
            SelectSignalsFolderByDefault();
            SelectSharedFolderByDefault();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            DrawNameInputField();

            _moduleSuffix = _selectedModuleType switch
            {
                ModuleType.Main => "",
                ModuleType.Test => "Test",
                ModuleType.Screen => "Screen",
                _ => throw new ArgumentOutOfRangeException()
            };

            CustomCheck("<size=12>Please, enter <b>module name</b>!</size>",
                _moduleName,
                $"<size=10><color=#ffdd00ff>Attention!</color></size><br>File name: <size=14><color=#ffdd00ff><u>{_moduleName}{_moduleSuffix}Module</u></color></size>");

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MODULE_TYPE_LABEL, EditorStyles.boldLabel, GUILayout.Width(90));

            ModuleType newModuleType = (ModuleType) EditorGUILayout.EnumPopup(_selectedModuleType, GUILayout.Width(310));
            if (newModuleType != _selectedModuleType)
            {
                _selectedModuleType = newModuleType;
                OnModuleTypeChanged();
            }

            ScriptableObject configFile = _directoryConfigMap[_selectedModuleType];
            EditorGUILayout.LabelField(CONFIG_TYPE_LABEL, EditorStyles.boldLabel, GUILayout.Width(45));
            EditorGUILayout.ObjectField(configFile, typeof(ScriptableObject), false, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();

            // One row, wide enough for all five toggles: Context, Root and Scene at 125 each leave
            // nothing over in the 400 this used to be, which is the only reason Signals ever sat
            // on a row of its own.
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(640));
            CreateToggles();
            CreateSignalsToggle();
            CreateSharedToggle();
            EditorGUILayout.EndHorizontal();

            if (_selectedModuleType == ModuleType.Screen)
                DrawScreenSettings();

            if (!string.IsNullOrEmpty(_moduleName))
                DisplayFolderStructurePreview();

            DisplayParentModuleSelection();

            if (_selectedModuleType == ModuleType.Screen)
            {
                DisplayActionsSection();
            }

            GUILayout.FlexibleSpace();
            DisplayCreateModuleButton();

            EditorGUILayout.EndVertical();
        }

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