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
    internal class CreateModelMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";

        private const float PANEL_HEADER_HEIGHT = 33f;

        private const string MODEL_NAME_LABEL = "Model Name: ";
        private const string CREATE_MODEL_BUTTON = "Create Model";
        private const string ADD_INJECTABLE_BUTTON = "Add Injectable";
        private const string INJECTABLES_LABEL = "Injectables:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_MODEL_NAME_TITLE = "Invalid Model Name";
        private const string INVALID_MODEL_NAME_MESSAGE = "Please enter a valid Model name.";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";
        private const string USE_DUMMY_BINDING_LABEL = "Create Dummy Model";
        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private string _modelName = string.Empty;
        private string _parentModulePath;
        private ModulePicker _picker;
        private ModuleRegistry _registry;
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private List<string> _injectableNames = new List<string>();
        private ModuleKind _selectedModuleKind;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private bool _useDummyBinding;
        private ED_CodeGenerator _codeGenSettings;

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();

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

            _bar.DrawWindow("Create Model", "FlowIoC", "An interface and the class behind it",
                null, null, "Create Model");

            _body.Begin(this);
            EditorGUILayout.LabelField(MODEL_NAME_LABEL, GUILayout.Width(100));
            _modelName = EditorGUILayout.TextField(_modelName);
            if (!string.IsNullOrEmpty(_modelName))
            {
                EditorGUILayout.LabelField($"{_modelName}Model", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(10);
            _useDummyBinding = EditorGUILayout.Toggle(USE_DUMMY_BINDING_LABEL, _useDummyBinding);
            EditorGUILayout.Space(10);

            DisplayInjectablesSection();
            DisplayParentModuleSelection();
            _body.End();

            DisplayCreateModelButton();
        }

        /// <summary>
        /// The module the model lands in, in the shape Add Shared or Signals asks the same question: a
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

            EditorGUILayout.BeginVertical();

            // CreateModelMenu never restricted which module kind could host a model.
            _picker.Draw(ref _parentModulePath, ref _selectedModuleName, _ => true, false);
            _selectedModuleKind = _picker.PickedKind;

            EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(_parentModulePath))
                EditorGUILayout.LabelField($"Selected: {Path.GetFileName(_parentModulePath)}", EditorStyles.boldLabel);
        }

        private void DisplayInjectablesSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(INJECTABLES_LABEL, EditorStyles.boldLabel);

            GUI.backgroundColor = new ModulePanelTheme().ActionAdd;
            if (GUILayout.Button(ADD_INJECTABLE_BUTTON))
            {
                _injectableNames.Add("NewInjectable");
            }

            GUI.backgroundColor = Color.white;

            // Noted here, dropped once the list has been drawn: leaving the loop from inside a row
            // ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _injectableNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _injectableNames[i] = EditorGUILayout.TextField(_injectableNames[i]);

                GUI.backgroundColor = new ModulePanelTheme().ActionRemove;

                if (GUILayout.Button("-", GUILayout.Width(30)))
                    removeAt = i;

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }


            if (removeAt >= 0)
                _injectableNames.RemoveAt(removeAt);
        }

        private void DisplayCreateModelButton()
        {
            bool inProgress = _generationState == GenerationState.InProgress;
            Color background = inProgress ? BUTTON_COLOR_IN_PROGRESS : new ModulePanelTheme().Action;

            if (!_body.FooterButton(this, CREATE_MODEL_BUTTON, inProgress || string.IsNullOrEmpty(_parentModulePath), background))
                return;

            if (string.IsNullOrEmpty(_modelName))
            {
                EditorUtility.DisplayDialog(INVALID_MODEL_NAME_TITLE, INVALID_MODEL_NAME_MESSAGE, "OK");
                return;
            }

            _generationState = GenerationState.InProgress;
            CreateModuleStructureForModelGeneration();
        }

        private void CreateModuleStructureForModelGeneration()
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

            Debug.Log($"[CreateModuleStructureForModelGeneration] Base Module Path: {baseModulePath}");

            // The pick is the module itself, whatever kind it is - a test module's own folder, not
            // the module it tests - so the file goes into the folder the pick names.
            string modulePath = baseModulePath;

            string modelPath = _configProvider.ConfigFor(_selectedModuleKind).FindFullFolderPathByID(FolderEVO.FolderType.Models, modulePath);
            string rootsAndContextsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);

            CreateModel(modelPath, moduleNamespace);
            string contextFile = FindContextFile(rootsAndContextsPath, _selectedModuleName, _selectedModuleKind);
            BindModelInContext(contextFile,
                _modelName + "Model",
                "I" + _modelName + "Model",
                _modelName + "DummyModel",
                moduleNamespace,
                _useDummyBinding);

            _generationState = GenerationState.Idle;
        }

        /// <summary>
        /// The module's context file. The kind decides what sits between the module name and the
        /// word Context, and the lookup covers the rest: a module whose Root roots a System or a
        /// Service names its context for that role, so the file may be PlayerSystemContext.cs.
        /// </summary>
        private string FindContextFile(string rootsAndContextsPath, string moduleName, ModuleKind moduleKind)
        {
            string kindSuffix = moduleKind switch
            {
                ModuleKind.Screen => "Screen",
                ModuleKind.Test => "Test",
                _ => string.Empty
            };

            return new ModuleContextFile().Find(rootsAndContextsPath, moduleName, kindSuffix);
        }

        private void CreateModel(string path, string moduleNamespace)
        {
            string modelName = _modelName + "Model";
            string dummyModelName = _modelName + "DummyModel";
            string modelInterfaceName = $"I{_modelName}" + "Model";

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            // Every script in a test module is wrapped in UNITY_EDITOR, the interface included.
            bool isTest = _selectedModuleKind == ModuleKind.Test;

            CodeGeneratorUtils.CreateModel(modelName, "TempModel", path, CodeGeneratorStrings.TempModelPath,
                moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Models]}", _injectableNames, false, isTest);
            if (_useDummyBinding)
                CodeGeneratorUtils.CreateModel(dummyModelName, "TempModel", path, CodeGeneratorStrings.TempModelPath,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Models]}", _injectableNames, true, isTest);
            CodeGeneratorUtils.CreateModelInterface(modelInterfaceName, "ITempModel", path, CodeGeneratorStrings.TempIModelPath,
                moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Models]}", isTest);

            EnsureNamespaceImport(modelName, path, "Models", moduleNamespace);
        }

        private void BindModelInContext(string contextPath, string modelName, string iModelName, string dummyModelName, string moduleNamespace,
            bool useDummyBinding)
        {
            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            CodeGeneratorUtils.BindModelInContext(contextPath, modelName, iModelName, dummyModelName,
                moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Models]}", useDummyBinding);
        }

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