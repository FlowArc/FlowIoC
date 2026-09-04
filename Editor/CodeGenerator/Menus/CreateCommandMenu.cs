#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    internal class CreateCommandMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";

        private const float PANEL_HEADER_HEIGHT = 33f;
        private const float MODULE_LIST_HEIGHT = 300f;

        private const string COMMAND_NAME_LABEL = "Command Name: ";
        private const string SIGNAL_LABEL = "Signal";
        private const string SIGNAL_CLASS_NAME_LABEL = "Class Name: ";
        private const string SIGNAL_NAME_LABEL = "Signal Name: ";
        private const string CREATE_COMMAND_BUTTON = "Create Command";
        private const string ADD_INJECTABLE_BUTTON = "Add Injectable";
        private const string INJECTABLES_LABEL = "Injectables:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_COMMAND_NAME_TITLE = "Invalid Command Name";
        private const string INVALID_COMMAND_NAME_MESSAGE = "Please enter a valid Command name.";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";
        private const float COMMAND_BUTTON_HEIGHT = 40;
        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;
        private static readonly Color BUTTON_COLOR_IDLE = Color.cyan;

        private static string _commandName;
        private static string _signalClassName;
        private static string _signalName;
        private static bool _isSequence;
        private static bool _isBind;
        private string _parentModulePath;
        private Dictionary<string, bool> _moduleExpandedState;
        private ModuleRegistry _registry;
        private Vector2 _scrollPosition;
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private Vector2 _injectablesScrollPosition;
        private List<string> _injectableNames = new List<string>();
        private ModuleKind _selectedModuleKind;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private ED_CodeGenerator _codeGenSettings;

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private enum GenerationState
        {
            Idle,
            InProgress,
            Completed
        }

        private void OnEnable()
        {
            _moduleExpandedState = new Dictionary<string, bool>();
            _parentModulePath = string.Empty;
            ED_CodeGenerator.CreateConfig();
            LoadCodeGeneratorSettings();
            _generationState = GenerationState.Idle;
            _registry = new ModuleRegistryFactory().FromProject();
        }

        private bool LoadCodeGeneratorSettings()
        {
            _codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (_codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            _bar.DrawWindow(FlowRole.Root, "Create Command", "FlowIoC", "One unit of work, bound to a signal",
                null, null, "Creating a Module");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(COMMAND_NAME_LABEL, GUILayout.Width(100));
            _commandName = EditorGUILayout.TextField(_commandName);
            if (!string.IsNullOrEmpty(_commandName))
            {
                EditorGUILayout.LabelField($"{_commandName}Command", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(10);

            DisplayBindToggleSection();

            if (_isBind)
            {
                DisplayTogglesSection();
                DisplaySignalEntrySection();
            }

            DisplayInjectablesSection();
            DisplayParentModuleSelection();
            GUILayout.FlexibleSpace();
            DisplayCreateCommandButton();
            EditorGUILayout.EndVertical();
        }

        private void DisplayBindToggleSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bind", GUILayout.Width(80));
            _isBind = EditorGUILayout.Toggle(_isBind, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The module the command lands in, in the shape Add Shared Data asks the same question: a
        /// bar in the Root's purple over a list tall enough to read, and the pick spelled out
        /// under it.
        /// </summary>
        private void DisplayParentModuleSelection()
        {
            EditorGUILayout.Space(10);

            var labelStyle = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            GUI.backgroundColor = new ModulePanelTheme().Header;
            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(PANEL_HEADER_HEIGHT));
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"),
                GUILayout.Width(35), GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.LabelField(PARENT_MODULE_LABEL, labelStyle, GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(MODULE_LIST_HEIGHT));
            EditorGUILayout.BeginVertical();

            // CreateCommandMenu never restricted which module kind could host a command.
            ModuleHierarchyDrawer.DrawModuleHierarchy(_registry, MODULES_PATH, 0, ref _moduleExpandedState, ref _parentModulePath,
                ref _selectedModuleName, _ => true);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_parentModulePath))
                EditorGUILayout.LabelField($"Selected: {Path.GetFileName(_parentModulePath)}", EditorStyles.boldLabel);
        }

        private void DisplayInjectablesSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(INJECTABLES_LABEL, EditorStyles.boldLabel);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(ADD_INJECTABLE_BUTTON))
            {
                _injectableNames.Add("NewInjectable");
            }

            GUI.backgroundColor = Color.white;

            float scrollViewHeight = Mathf.Min(150, 30 * _injectableNames.Count);
            _injectablesScrollPosition = EditorGUILayout.BeginScrollView(_injectablesScrollPosition, GUILayout.Height(scrollViewHeight));
            // Noted here, dropped once the list has been drawn: leaving the loop from inside a row
            // ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _injectableNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _injectableNames[i] = EditorGUILayout.TextField(_injectableNames[i]);

                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("-", GUILayout.Width(30)))
                    removeAt = i;

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (removeAt >= 0)
                _injectableNames.RemoveAt(removeAt);
        }

        private void DisplayCreateCommandButton()
        {
            GUI.backgroundColor = _generationState == GenerationState.InProgress ? BUTTON_COLOR_IN_PROGRESS : BUTTON_COLOR_IDLE;
            EditorGUI.BeginDisabledGroup(_generationState == GenerationState.InProgress || string.IsNullOrEmpty(_parentModulePath));

            if (GUILayout.Button(CREATE_COMMAND_BUTTON, GUILayout.Height(COMMAND_BUTTON_HEIGHT)))
            {
                if (string.IsNullOrEmpty(_commandName))
                {
                    EditorUtility.DisplayDialog(INVALID_COMMAND_NAME_TITLE, INVALID_COMMAND_NAME_MESSAGE, "OK");
                    return;
                }

                _generationState = GenerationState.InProgress;
                CreateModuleStructureForCommandGeneration();
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }

        private void DisplayTogglesSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Is Sequel", GUILayout.Width(80));
            _isSequence = EditorGUILayout.Toggle(_isSequence, GUILayout.Width(20));

            // GUILayout.Label("Is Pre-Binded", GUILayout.Width(80));
            // _isPreBinded = EditorGUILayout.Toggle(_isPreBinded, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }

        private void DisplaySignalEntrySection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(SIGNAL_LABEL, boldLabelStyle, GUILayout.Width(50));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(SIGNAL_CLASS_NAME_LABEL, GUILayout.Width(75));
            _signalClassName = EditorGUILayout.TextField(_signalClassName);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(SIGNAL_NAME_LABEL, GUILayout.Width(75));
            _signalName = EditorGUILayout.TextField(_signalName);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void CreateModuleStructureForCommandGeneration()
        {
            if (_selectedModuleKind != ModuleKind.Main && string.IsNullOrEmpty(_parentModulePath))
            {
                EditorUtility.DisplayDialog(PARENT_MODULE_REQUIRED_TITLE, PARENT_MODULE_REQUIRED_MESSAGE, "OK");
                _generationState = GenerationState.Idle;
                return;
            }

            string baseModulePath = string.IsNullOrEmpty(_parentModulePath)
                ? Path.Combine(Application.dataPath, MODULES_PATH)
                : _parentModulePath;

            if (!baseModulePath.EndsWith($"{_selectedModuleName}Module", StringComparison.OrdinalIgnoreCase))
            {
                baseModulePath = Path.Combine(baseModulePath, $"{_selectedModuleName}Module");
            }

            Debug.Log($"[CreateModuleStructureForCommandGeneration] Base Module Path: {baseModulePath}");

            string subDirectory = _selectedModuleKind switch
            {
                ModuleKind.Sub => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules],
                ModuleKind.Test => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules],
                ModuleKind.Screen => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules],
                _ => string.Empty
            };

            string modulePath = string.IsNullOrEmpty(subDirectory)
                ? baseModulePath
                : Path.Combine(baseModulePath, subDirectory);

            string commandPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.Controllers, modulePath);
            string rootsAndContextsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);

            CreateCommand(commandPath, moduleNamespace);

            if (_isBind)
            {
                InjectSignalInContext(rootsAndContextsPath);
                BindCommandInContext(rootsAndContextsPath, moduleNamespace);
            }

            _generationState = GenerationState.Idle;
        }

        private void CreateCommand(string path, string moduleNamespace)
        {
            string commandName = _commandName + "Command";

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            CodeGeneratorUtils.CreateCommand(commandName, "TempCommand", path, CodeGeneratorStrings.TempCommandPath,
                moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers]}", _injectableNames);

            EnsureNamespaceImport(commandName, path, $"{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers]}",
                moduleNamespace);
        }

        private void InjectSignalInContext(string contextPath)
        {
            string signalClassName = _signalClassName;
            CodeGeneratorUtils.InjectSignalInContext(ContextFile(contextPath), signalClassName);
        }

        /// <summary>
        /// The using the context needs is the namespace the command was actually written into, so
        /// it is read off the same Controllers folder <see cref="CreateCommand"/> writes to. A
        /// hardcoded ".Commands" left the context importing a namespace no module has.
        /// </summary>
        private void BindCommandInContext(string contextPath, string moduleNamespace)
        {
            string commandName = _commandName + "Command";
            string signalClassName = _signalClassName;
            string signalName = _signalName;

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            string commandNamespace =
                $"{moduleNamespace}.{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers]}";

            CodeGeneratorUtils.BindCommandInContext(ContextFile(contextPath), commandName, signalClassName, signalName,
                commandNamespace, _isSequence);
        }

        /// <summary>
        /// The module's context, whatever it is called. A module whose Root roots a System or a
        /// Service names its context for that role, so the file is looked up rather than assumed.
        /// </summary>
        private string ContextFile(string rootsAndContextsPath) =>
            new ModuleContextFile().Find(rootsAndContextsPath, _selectedModuleName);

        private void EnsureNamespaceImport(string className, string path, string type, string moduleNamespace)
        {
            string filePath = path + "/" + className + ".cs";
            string[] fileLines = File.ReadAllLines(filePath);
            string namespaceLine = "using " + moduleNamespace + $".{type};";
            if (!Array.Exists(fileLines, line => line.Contains(namespaceLine)))
            {
                List<string> newLines = new List<string>(fileLines);
                newLines.Insert(1, namespaceLine);
                File.WriteAllLines(filePath, newLines);
                AssetDatabase.Refresh();
                _generationState = GenerationState.Completed;
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (_generationState == GenerationState.InProgress)
            {
                _generationState = GenerationState.Completed;
            }
        }
    }
}
#endif