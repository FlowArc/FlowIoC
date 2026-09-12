#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Help;
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

        private const string COMMAND_NAME_LABEL = "Command Name: ";
        private const string BINDING_LABEL = "How it is bound:";
        private const string BINDING_CAPTION = "Into the module's Context, where the flow reads right";
        private const string CREATE_COMMAND_BUTTON = "Create Command";
        private const string ADD_INJECTABLE_BUTTON = "Add Injectable";
        private const string INJECTABLES_LABEL = "Injectables:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_COMMAND_NAME_TITLE = "Invalid Command Name";
        private const string INVALID_COMMAND_NAME_MESSAGE = "Please enter a valid Command name.";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";
        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private string _commandName = string.Empty;
        private string _parentModulePath;
        private ModulePicker _picker;
        private ModuleRegistry _registry;
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private List<string> _injectableNames = new List<string>();
        private ModuleKind _selectedModuleKind;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private ED_CodeGenerator _codeGenSettings;

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();
        private readonly GeneratorNamePreview _preview = new GeneratorNamePreview();
        private readonly GeneratorListBar _listBar = new GeneratorListBar();
        private readonly CommandBindingSnippet _binding = new CommandBindingSnippet();

        private HelpPainter _painter;

        private enum GenerationState
        {
            Idle,
            InProgress,
            Completed
        }

        private void OnEnable()
        {
            _parentModulePath = string.Empty;
            ED_CodeGenerator.CreateConfig();
            LoadCodeGeneratorSettings();
            _generationState = GenerationState.Idle;
            _registry = new ModuleRegistryFactory().FromProject();
            _picker = new ModulePicker(_registry);

            // Without this the window is sent no MouseMove events at all, and a row of the picker
            // would only light up when something else happened to repaint it.
            wantsMouseMove = true;
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
            if (Event.current.type == EventType.MouseMove) Repaint();

            _bar.DrawWindow("Create Command", "FlowIoC", "One unit of work, bound to a signal",
                null, null, "Create Command");

            _body.Begin(this);
            _commandName = _preview.Draw(COMMAND_NAME_LABEL, _commandName, _commandName + "Command");

            DisplayBindingPreview();
            DisplayInjectablesSection();
            DisplayParentModuleSelection();
            _body.End();

            DisplayCreateCommandButton();
        }

        /// <summary>
        /// The binding the Context needs for this command, drawn as the Help window draws a
        /// snippet - the same colouring, and the same Copy button beside it - and spelled with the
        /// holder field the picked module's Context declares. It is shown rather than written:
        /// where a command sits in a sequence is a decision about the flow, and the window used
        /// to take it from two names typed as text that nothing checked.
        /// </summary>
        private void DisplayBindingPreview()
        {
            if (string.IsNullOrEmpty(_commandName)) return;

            _painter ??= new HelpPainter(new HelpTheme());

            PanelHeader(BINDING_LABEL);

            _painter.Code(_binding.For(_commandName, HolderField()), BINDING_CAPTION);
        }

        /// <summary>
        /// The picked module's Context, asked for the field its holder lives in. The conventional
        /// name until a module is picked - the snippet is drawn before the list is.
        /// </summary>
        private string HolderField()
        {
            if (string.IsNullOrEmpty(_parentModulePath)) return _binding.HolderFieldIn(null);

            string rootsAndContextsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, _parentModulePath);

            return _binding.HolderFieldIn(ContextFile(rootsAndContextsPath));
        }

        /// <summary>
        /// The bar a panel in this window wears: the Root's purple, an icon, and the panel's name,
        /// the same one the parent-module list carries below.
        /// </summary>
        private void PanelHeader(string label)
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
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// The module the command lands in, in the shape Add Shared or Signals asks the same question: a
        /// bar in the Root's purple over a list tall enough to read, and the pick spelled out
        /// under it.
        /// </summary>
        private void DisplayParentModuleSelection()
        {
            _picker.DrawBar(PARENT_MODULE_LABEL, _parentModulePath);

            EditorGUILayout.BeginVertical();

            // CreateCommandMenu never restricted which module kind could host a command.
            _picker.Draw(ref _parentModulePath, ref _selectedModuleName, _ => true, false);
            _selectedModuleKind = _picker.PickedKind;

            EditorGUILayout.EndVertical();
        }

        private void DisplayInjectablesSection()
        {
            if (_listBar.Draw(INJECTABLES_LABEL, ADD_INJECTABLE_BUTTON))
                _injectableNames.Add("NewInjectable");

            // Noted here, dropped once the list has been drawn: leaving the loop from inside a row
            // ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _injectableNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _injectableNames[i] = EditorGUILayout.TextField(_injectableNames[i]);

                if (_listBar.DrawRemove())
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
            }


            if (removeAt >= 0)
                _injectableNames.RemoveAt(removeAt);
        }

        private void DisplayCreateCommandButton()
        {
            bool inProgress = _generationState == GenerationState.InProgress;
            Color background = inProgress ? BUTTON_COLOR_IN_PROGRESS : new ModulePanelTheme().Action;

            if (!_body.FooterButton(this, CREATE_COMMAND_BUTTON, inProgress || string.IsNullOrEmpty(_parentModulePath), background))
                return;

            if (string.IsNullOrEmpty(_commandName))
            {
                EditorUtility.DisplayDialog(INVALID_COMMAND_NAME_TITLE, INVALID_COMMAND_NAME_MESSAGE, "OK");
                return;
            }

            _generationState = GenerationState.InProgress;
            CreateModuleStructureForCommandGeneration();
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

            // The pick is the module itself, whatever kind it is - a test module's own folder, not
            // the module it tests - so the file goes into the folder the pick names.
            string modulePath = baseModulePath;

            string commandPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.Controllers, modulePath);
            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);

            CreateCommand(commandPath, moduleNamespace);

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
                moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers]}", _injectableNames,
                _selectedModuleKind == ModuleKind.Test);

            EnsureNamespaceImport(commandName, path, $"{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers]}",
                moduleNamespace);
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