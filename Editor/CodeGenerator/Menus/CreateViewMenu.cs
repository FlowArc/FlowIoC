#if UNITY_EDITOR
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
        private const string ISTEST_LABEL = "IsTest: ";
        private const float VIEW_BUTTON_HEIGHT = 40;
        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;
        private static readonly Color BUTTON_COLOR_IDLE = Color.cyan;

        private static string _viewName;
        private string _parentModulePath;
        private Dictionary<string, bool> _moduleExpandedState;
        private ModuleRegistry _registry;
        private Vector2 _scrollPosition;
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private Vector2 _actionScrollPosition;
        private List<string> _actionNames = new List<string>();
        private bool _isTest;
        private ModuleKind _selectedModuleKind;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private ED_CodeGenerator _codeGenSettings;

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
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(VIEW_NAME_LABEL, GUILayout.Width(100));
            _viewName = EditorGUILayout.TextField(_viewName);
            if (!string.IsNullOrEmpty(_viewName))
            {
                EditorGUILayout.LabelField($"{_viewName}View", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(ISTEST_LABEL);
            _isTest = EditorGUILayout.Toggle(_isTest);

            DisplayActionsSection();
            DisplayParentModuleSelection();
            DisplayCreateViewButton();
            EditorGUILayout.EndVertical();
        }

        private void DisplayParentModuleSelection()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(PARENT_MODULE_LABEL);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            EditorGUILayout.BeginVertical("box");

            // Exact-kind filter: a regular view's parent may be anything but Test; a test
            // view's parent must be Test and nothing else.
            ModuleHierarchyDrawer.DrawModuleHierarchy(_registry, MODULES_PATH, 0, ref _moduleExpandedState, ref _parentModulePath,
                ref _selectedModuleName, parent => _isTest ? parent == ModuleKind.Test : parent != ModuleKind.Test);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(20);
        }

        private void DisplayActionsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(ACTIONS_LABEL, EditorStyles.boldLabel);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(ADD_ACTION_BUTTON))
            {
                _actionNames.Add("NewAction");
            }

            GUI.backgroundColor = Color.white;

            float scrollViewHeight = Mathf.Min(150, 30 * _actionNames.Count);
            _actionScrollPosition = EditorGUILayout.BeginScrollView(_actionScrollPosition, GUILayout.Height(scrollViewHeight));
            for (int i = 0; i < _actionNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _actionNames[i] = EditorGUILayout.TextField(_actionNames[i]);

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    _actionNames.RemoveAt(i);
                    break;
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DisplayCreateViewButton()
        {
            GUI.backgroundColor = _generationState == GenerationState.InProgress ? BUTTON_COLOR_IN_PROGRESS : BUTTON_COLOR_IDLE;
            EditorGUI.BeginDisabledGroup(_generationState == GenerationState.InProgress || string.IsNullOrEmpty(_parentModulePath));

            if (GUILayout.Button(CREATE_VIEW_BUTTON, GUILayout.Height(VIEW_BUTTON_HEIGHT)))
            {
                if (string.IsNullOrEmpty(_viewName))
                {
                    EditorUtility.DisplayDialog(INVALID_VIEW_NAME_TITLE, INVALID_VIEW_NAME_MESSAGE, "OK");
                    return;
                }

                _generationState = GenerationState.InProgress;
                CreateModuleStructureForViewGeneration();
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
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

            string viewsAndMediatorsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.ViewsAndMediators, modulePath);
            string rootsAndContextsPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);


            CreateViewAndMediator(viewsAndMediatorsPath, _isTest, moduleNamespace);
            BindMediationInContext(rootsAndContextsPath, _isTest, moduleNamespace);

            _generationState = GenerationState.Idle;
        }


        private void CreateViewAndMediator(string path, bool isTest, string moduleNamespace)
        {
            string suffix = isTest ? "Test" : "";
            string viewName = _viewName + suffix + "View";
            string mediatorName = _viewName + suffix + "Mediator";

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            if (isTest)
            {
                CodeGeneratorUtils.CreateView(viewName, "TempView", path, CodeGeneratorStrings.TempViewPath,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}",
                    _actionNames, true);
                CodeGeneratorUtils.CreateMediator(mediatorName, viewName, "TempMediator", path, CodeGeneratorStrings.TempMediatorPath,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}",
                    _actionNames, true);
            }
            else
            {
                CodeGeneratorUtils.CreateView(viewName, "TempView", path, CodeGeneratorStrings.TempViewPath,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}", _actionNames,
                    false);
                CodeGeneratorUtils.CreateMediator(mediatorName, viewName, "TempMediator", path, CodeGeneratorStrings.TempMediatorPath,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}",
                    _actionNames, false);
            }

            EnsureNamespaceImport(mediatorName, path, isTest,
                $"{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}", moduleNamespace);
            EnsureNamespaceImport(viewName, path, isTest, $"{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}",
                moduleNamespace);
        }

        private void BindMediationInContext(string contextPath, bool isTest, string moduleNamespace)
        {
            string suffix = isTest ? "Test" : "";
            string viewName = _viewName + suffix + "View";
            string mediatorName = _viewName + suffix + "Mediator";
            string contextName = _selectedModuleName + suffix + "Context";

            ED_CodeGenerator codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            if (isTest)
            {
                CodeGeneratorUtils.BindMediationInContext(contextPath + "/" + contextName + ".cs", viewName, mediatorName,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}");
            }
            else
            {
                CodeGeneratorUtils.BindMediationInContext(contextPath + "/" + contextName + ".cs", viewName, mediatorName,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ViewsAndMediators]}");
            }
        }

        private void EnsureNamespaceImport(string className, string path, bool isTest, string type, string moduleNamespace)
        {
            string filePath = path + "/" + className + ".cs";
            string[] fileLines = File.ReadAllLines(filePath);
            string namespaceLine = isTest ? "using " + moduleNamespace + $".Tests.{type};" : "using " + moduleNamespace + $".{type};";
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