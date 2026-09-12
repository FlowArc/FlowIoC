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
    internal class CreateViewMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";

        private const string VIEW_NAME_LABEL = "View Name: ";
        private const string CREATE_VIEW_BUTTON = "Create View";
        private const string ADD_ACTION_BUTTON = "Add Action";
        private const string ACTIONS_LABEL = "Actions:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_VIEW_NAME_TITLE = "Invalid View Name";
        private const string INVALID_VIEW_NAME_MESSAGE = "Please enter a valid View name.";
        private const string PARENT_MODULE_REQUIRED_TITLE = "Parent Module Required";
        private const string PARENT_MODULE_REQUIRED_MESSAGE = "Please select a parent module";
        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private string _viewName = string.Empty;
        private string _parentModulePath;
        private ModulePicker _picker;
        private ModuleRegistry _registry;
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private List<string> _actionNames = new List<string>();
        private ModuleKind _selectedModuleKind;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private ED_CodeGenerator _codeGenSettings;

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();
        private readonly GeneratorNamePreview _preview = new GeneratorNamePreview();
        private readonly GeneratorListBar _listBar = new GeneratorListBar();

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

            _bar.DrawWindow("Create View", "FlowIoC", "A View and the Mediator that drives it",
                null, null, "Create View");

            _body.Begin(this);
            _viewName = _preview.Draw(VIEW_NAME_LABEL, _viewName, _viewName + "View");

            DisplayActionsSection();
            DisplayParentModuleSelection();
            _body.End();

            DisplayCreateViewButton();
        }

        /// <summary>
        /// The module the view lands in, in the shape Add Shared or Signals asks the same question: a
        /// bar in the Root's purple over a list tall enough to read, and the pick spelled out
        /// under it.
        /// </summary>
        private void DisplayParentModuleSelection()
        {
            _picker.DrawBar(PARENT_MODULE_LABEL, _parentModulePath);

            EditorGUILayout.BeginVertical();

            // Any module may hold a view. A test module's view is wrapped in UNITY_EDITOR like the
            // rest of the module, and the pick is what says so - there is no toggle to agree with.
            _picker.Draw(ref _parentModulePath, ref _selectedModuleName, _ => true, false);
            _selectedModuleKind = _picker.PickedKind;

            EditorGUILayout.EndVertical();
        }

        private void DisplayActionsSection()
        {
            if (_listBar.Draw(ACTIONS_LABEL, ADD_ACTION_BUTTON))
                _actionNames.Add("NewAction");

            // Noted here, dropped once the list has been drawn: leaving the loop from inside a row
            // ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _actionNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _actionNames[i] = EditorGUILayout.TextField(_actionNames[i]);

                if (_listBar.DrawRemove())
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
            }


            if (removeAt >= 0)
                _actionNames.RemoveAt(removeAt);
        }

        private void DisplayCreateViewButton()
        {
            bool inProgress = _generationState == GenerationState.InProgress;
            Color background = inProgress ? BUTTON_COLOR_IN_PROGRESS : new ModulePanelTheme().Action;

            if (!_body.FooterButton(this, CREATE_VIEW_BUTTON, inProgress || string.IsNullOrEmpty(_parentModulePath), background))
                return;

            if (string.IsNullOrEmpty(_viewName))
            {
                EditorUtility.DisplayDialog(INVALID_VIEW_NAME_TITLE, INVALID_VIEW_NAME_MESSAGE, "OK");
                return;
            }

            _generationState = GenerationState.InProgress;
            CreateModuleStructureForViewGeneration();
        }

        private void CreateModuleStructureForViewGeneration()
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

            Debug.Log($"[CreateModuleStructureForViewGeneration] Base Module Path: {baseModulePath}");

            // The pick is the module itself, whatever kind it is - a test module's own folder, not
            // the module it tests - so the file goes into the folder the pick names.
            string modulePath = baseModulePath;

            string viewsAndMediatorsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.ViewsAndMediators, modulePath);
            string rootsAndContextsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);


            bool isTest = _selectedModuleKind == ModuleKind.Test;

            CreateViewAndMediator(viewsAndMediatorsPath, isTest, moduleNamespace);
            BindMediationInContext(rootsAndContextsPath, moduleNamespace);

            _generationState = GenerationState.Idle;
        }


        /// <summary>
        /// The view and the mediator, into the module's ViewsMediators folder under the namespace
        /// that folder gives them. A test module gets the same two files wrapped in UNITY_EDITOR:
        /// its namespace comes off its folder like any other module's, and the name is the view's
        /// own - the module is the test, not the view.
        /// </summary>
        private void CreateViewAndMediator(string path, bool isTest, string moduleNamespace)
        {
            string viewName = _viewName + "View";
            string mediatorName = _viewName + "Mediator";

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            string folder = codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators];
            string viewsNamespace = moduleNamespace + "." + folder;

            CodeGeneratorUtils.CreateView(viewName, "TempView", path, CodeGeneratorStrings.TempViewPath,
                viewsNamespace, _actionNames, isTest);
            CodeGeneratorUtils.CreateMediator(mediatorName, viewName, "TempMediator", path, CodeGeneratorStrings.TempMediatorPath,
                viewsNamespace, _actionNames, isTest);

            EnsureNamespaceImport(mediatorName, path, folder, moduleNamespace);
            EnsureNamespaceImport(viewName, path, folder, moduleNamespace);
        }

        private void BindMediationInContext(string contextPath, string moduleNamespace)
        {
            string viewName = _viewName + "View";
            string mediatorName = _viewName + "Mediator";

            // A module whose Root roots a System or a Service names its context for that role, so
            // the file is looked up rather than assumed to be {module}Context.cs. A test module's
            // name already ends in Test, so its context is found under its own name.
            string contextFile = new ModuleContextFile().Find(contextPath, _selectedModuleName);

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            CodeGeneratorUtils.BindMediationInContext(contextFile, viewName, mediatorName,
                moduleNamespace + "." + codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]);
        }

        private void EnsureNamespaceImport(string className, string path, string type, string moduleNamespace)
        {
            string filePath = path + "/" + className + ".cs";
            string[] fileLines = File.ReadAllLines(filePath);
            string namespaceLine = "using " + moduleNamespace + "." + type + ";";
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