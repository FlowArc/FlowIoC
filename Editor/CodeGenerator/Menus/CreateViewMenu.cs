#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    internal class CreateViewMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";
        private const string MODULE_INFO_FILE = "_module_info.txt";
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
        private Vector2 _scrollPosition;
        private Dictionary<ModuleType, DirectoryStructureConfig> _directoryConfigMap;
        private Vector2 _actionScrollPosition;
        private List<string> _actionNames = new List<string>();
        private bool _isTest;
        private ModuleType _selectedModuleType;
        private static GenerationState _generationState;
        private string _selectedModuleName = string.Empty;
        private CodeGeneratorSettings _codeGenSettings;

        private enum ModuleType
        {
            Main,
            Sub,
            Test,
            Screen
        }

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
            InitializeConfigMap();
            LoadCodeGeneratorSettings();
            _generationState = GenerationState.Idle;
        }

        private void InitializeConfigMap()
        {
            CodeGeneratorSettings.CreateConfig();
            _directoryConfigMap = new Dictionary<ModuleType, DirectoryStructureConfig>
            {
                {ModuleType.Main, MainModuleDirectoryStructureConfig.GetOrCreateConfig("Main")},
                {ModuleType.Screen, ScreenModuleDirectoryStructureConfig.GetOrCreateConfig("Screen")},
                {ModuleType.Test, TestModuleDirectoryStructureConfig.GetOrCreateConfig("Test")}
            };
        }

        private bool LoadCodeGeneratorSettings()
        {
            _codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (_codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
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
            DrawModuleHierarchy(MODULES_PATH, 0);
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

        private void DrawModuleHierarchy(string path, int indentLevel)
        {
            if (!Directory.Exists(Application.dataPath + "/" + path)) return;
            string[] directories = Directory.GetDirectories(Application.dataPath + "/" + path);
            foreach (string directory in directories)
            {
                string moduleInfoPath = Path.Combine(directory, MODULE_INFO_FILE);
                if (File.Exists(moduleInfoPath))
                {
                    string directoryName = Path.GetFileName(directory);
                    string moduleTypePostfix = GetModuleTypePostfix(moduleInfoPath);
                    string displayName = directoryName + moduleTypePostfix;
                    _moduleExpandedState.TryAdd(directory, false);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(indentLevel * 10);
                    _moduleExpandedState[directory] = EditorGUILayout.Foldout(_moduleExpandedState[directory], displayName, true, new GUIStyle(EditorStyles.foldout) {richText = true});

                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        normal = {textColor = _parentModulePath == directory ? Color.green : GUI.skin.button.normal.textColor}
                    };
                    string buttonText = _parentModulePath == directory ? "Selected" : "Select";

                    EditorGUI.BeginDisabledGroup(!CanSelect(moduleTypePostfix));
                    if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                    {
                        if (_parentModulePath == directory)
                        {
                            _parentModulePath = string.Empty;
                            _selectedModuleName = string.Empty;
                        }
                        else
                        {
                            _parentModulePath = directory;
                            _selectedModuleName = GetModuleNameFromInfoFile(moduleInfoPath);
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();

                    if (_moduleExpandedState[directory])
                    {
                        EditorGUI.indentLevel++;
                        DrawModuleHierarchy(path + "/" + directoryName, indentLevel + 1);
                        DrawSubModulesRecursively(directory, indentLevel + 1);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private string GetModuleNameFromInfoFile(string moduleInfoPath)
        {
            foreach (string line in File.ReadAllLines(moduleInfoPath))
            {
                if (line.StartsWith("ModuleName: "))
                {
                    string moduleName = line.Substring("ModuleName: ".Length).Trim();

                    if (moduleName.EndsWith("Module"))
                    {
                        return moduleName.Substring(0, moduleName.Length - "Module".Length).Trim();
                    }

                    return moduleName;
                }
            }

            return string.Empty;
        }

        private bool CanSelect(string moduleTypePostfix)
        {
            return _isTest switch
            {
                false => moduleTypePostfix.Contains("(Main)") || moduleTypePostfix.Contains("(Sub)") || moduleTypePostfix.Contains("(Screen)"),
                true => moduleTypePostfix.Contains("(Test)")
            };
        }


        private void DrawSubModulesRecursively(string modulePath, int indentLevel)
        {
            string[] subModulePaths =
            {
                _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules], _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules],
                _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules]
            };
            foreach (string subPath in subModulePaths)
            {
                string subDirectoryPath = Path.Combine(modulePath, subPath);
                if (Directory.Exists(subDirectoryPath))
                {
                    string[] subDirectories = Directory.GetDirectories(subDirectoryPath);
                    foreach (string subDirectory in subDirectories)
                    {
                        string moduleInfoPath = Path.Combine(subDirectory, MODULE_INFO_FILE);
                        if (File.Exists(moduleInfoPath))
                        {
                            string directoryName = Path.GetFileName(subDirectory);
                            string moduleTypePostfix = GetModuleTypePostfix(moduleInfoPath);
                            string displayName = directoryName + moduleTypePostfix;
                            _moduleExpandedState.TryAdd(subDirectory, false);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(indentLevel * 15);
                            _moduleExpandedState[subDirectory] = EditorGUILayout.Foldout(_moduleExpandedState[subDirectory], displayName, true, new GUIStyle(EditorStyles.foldout) {richText = true});

                            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                            {
                                normal = {textColor = _parentModulePath == subDirectory ? Color.green : GUI.skin.button.normal.textColor}
                            };
                            string buttonText = _parentModulePath == subDirectory ? "Selected" : "Select";

                            EditorGUI.BeginDisabledGroup(!CanSelect(moduleTypePostfix));
                            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                            {
                                if (_parentModulePath == subDirectory)
                                {
                                    _parentModulePath = string.Empty;
                                    _selectedModuleName = string.Empty;
                                }
                                else
                                {
                                    _parentModulePath = subDirectory;
                                    _selectedModuleName = GetModuleNameFromInfoFile(moduleInfoPath);
                                }
                            }

                            EditorGUI.EndDisabledGroup();

                            EditorGUILayout.EndHorizontal();

                            if (_moduleExpandedState[subDirectory])
                            {
                                DrawSubModulesRecursively(subDirectory, indentLevel + 1);
                            }
                        }
                    }
                }
            }
        }

        private string GetModuleTypePostfix(string moduleInfoPath)
        {
            string moduleTypePostfix = string.Empty;
            foreach (string line in File.ReadAllLines(moduleInfoPath))
            {
                if (line.StartsWith("ModuleType: "))
                {
                    string typeString = line.Substring("ModuleType: ".Length);
                    if (Enum.TryParse(typeString, out ModuleType moduleType))
                    {
                        moduleTypePostfix = $" <b>({moduleType})</b>";
                    }
                }
            }

            return moduleTypePostfix;
        }

        private void CreateModuleStructureForViewGeneration()
        {
            if (_selectedModuleType != ModuleType.Main && string.IsNullOrEmpty(_parentModulePath))
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

            string subDirectory = _selectedModuleType switch
            {
                ModuleType.Sub => _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules],
                ModuleType.Test => _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules],
                ModuleType.Screen => _codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules],
                _ => string.Empty
            };

            string modulePath = string.IsNullOrEmpty(subDirectory)
                ? baseModulePath
                : Path.Combine(baseModulePath, subDirectory);

            string viewsAndMediatorsPath = _directoryConfigMap[_selectedModuleType].FindFullFolderPathByID(FolderConfig.FolderType.ViewsAndMediators, modulePath);
            string rootsAndContextsPath = _directoryConfigMap[_selectedModuleType].FindFullFolderPathByID(FolderConfig.FolderType.RootsAndContexts, modulePath);
            string moduleNamespace = GetModuleNamespace(modulePath);


            CreateViewAndMediator(viewsAndMediatorsPath, _isTest, moduleNamespace);
            BindMediationInContext(rootsAndContextsPath, _isTest, moduleNamespace);

            _generationState = GenerationState.Idle;
        }


        private void CreateViewAndMediator(string path, bool isTest, string moduleNamespace)
        {
            string suffix = isTest ? "Test" : "";
            string viewName = _viewName + suffix + "View";
            string mediatorName = _viewName + suffix + "Mediator";

            CodeGeneratorSettings codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            if (isTest)
            {
                CodeGeneratorUtils.CreateView(viewName, "TempView", path, CodeGeneratorStrings.TempViewPath,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}", _actionNames, true);
                CodeGeneratorUtils.CreateMediator(mediatorName, viewName, "TempMediator", path, CodeGeneratorStrings.TempMediatorPath,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}",
                    _actionNames, true);
            }
            else
            {
                CodeGeneratorUtils.CreateView(viewName, "TempView", path, CodeGeneratorStrings.TempViewPath,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}", _actionNames, false);
                CodeGeneratorUtils.CreateMediator(mediatorName, viewName, "TempMediator", path, CodeGeneratorStrings.TempMediatorPath,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}",
                    _actionNames, false);
            }

            EnsureNamespaceImport(mediatorName, path, isTest, $"{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}", moduleNamespace);
            EnsureNamespaceImport(viewName, path, isTest, $"{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}", moduleNamespace);
        }

        private void BindMediationInContext(string contextPath, bool isTest, string moduleNamespace)
        {
            string suffix = isTest ? "Test" : "";
            string viewName = _viewName + suffix + "View";
            string mediatorName = _viewName + suffix + "Mediator";
            string contextName = _selectedModuleName + suffix + "Context";
            
            CodeGeneratorSettings codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            if (isTest)
            {
                CodeGeneratorUtils.BindMediationInContext(contextPath + "/" + contextName + ".cs", viewName, mediatorName,
                    moduleNamespace + $".Tests.{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}");
            }
            else
            {
                CodeGeneratorUtils.BindMediationInContext(contextPath + "/" + contextName + ".cs", viewName, mediatorName,
                    moduleNamespace + $".{codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ViewsAndMediators]}");
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

        private string GetModuleNamespace(string modulePath)
        {
            List<string> moduleNames = new List<string>();

            string currentPath = modulePath;

            while (true)
            {
                string moduleInfoPath = Path.Combine(currentPath, MODULE_INFO_FILE);
                if (File.Exists(moduleInfoPath))
                {
                    string moduleName = "";
                    string moduleType = "";

                    foreach (string line in File.ReadAllLines(moduleInfoPath))
                    {
                        if (line.StartsWith("ModuleName: "))
                        {
                            moduleName = line.Substring("ModuleName: ".Length).Trim();

                            if (moduleName.EndsWith("Module"))
                            {
                                moduleName = moduleName.Substring(0, moduleName.Length - "Module".Length).Trim();
                            }
                        }
                        else if (line.StartsWith("ModuleType: "))
                        {
                            moduleType = line.Substring("ModuleType: ".Length).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(moduleName))
                    {
                        moduleNames.Insert(0, moduleName + "Module");
                    }

                    if (moduleType == "Main")
                    {
                        break;
                    }
                    else
                    {
                        DirectoryInfo parentDir = Directory.GetParent(currentPath);
                        if (parentDir == null || string.IsNullOrEmpty(parentDir.FullName))
                        {
                            break;
                        }

                        currentPath = parentDir.FullName;
                    }
                }
                else
                {
                    DirectoryInfo parentDir = Directory.GetParent(currentPath);
                    if (parentDir == null || string.IsNullOrEmpty(parentDir.FullName))
                    {
                        break;
                    }

                    currentPath = parentDir.FullName;
                }
            }

            return "Modules." + string.Join(".", moduleNames);
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