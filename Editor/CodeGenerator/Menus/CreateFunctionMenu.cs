#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// Create Function, beside Create Command. It writes one file into the module's Controllers
    /// folder and nothing else: a function is called from inside a Command rather than dispatched,
    /// so there is no signal to declare and no Context to bind it in.
    ///
    /// What it exists for is the arity. A function derives from one of the shipped base types and
    /// never from FunctionBody, and picking the base by hand is where a hand-written function used
    /// to go wrong - the parameters, the return type and the base's generic arguments all have to
    /// agree, and the window is what keeps them agreeing.
    /// </summary>
    internal class CreateFunctionMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";

        private const float PANEL_HEADER_HEIGHT = 33f;

        private const string FUNCTION_NAME_LABEL = "Function Name: ";
        private const string CREATE_FUNCTION_BUTTON = "Create Function";
        private const string ADD_PARAMETER_BUTTON = "Add Parameter";
        private const string ADD_INJECTABLE_BUTTON = "Add Injectable";
        private const string PARAMETERS_LABEL = "Execute Parameters:";
        private const string INJECTABLES_LABEL = "Injectables:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string INVALID_FUNCTION_NAME_TITLE = "Invalid Function Name";
        private const string INVALID_FUNCTION_NAME_MESSAGE = "Please enter a valid Function name.";

        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private static string _functionName;
        private static FunctionKind _kind;
        private static string _returnType = "double";
        private static GenerationState _generationState;

        private readonly List<FunctionParameter> _parameters = new List<FunctionParameter>();
        private readonly List<string> _injectableNames = new List<string>();
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly FunctionScriptWriter _writer = new FunctionScriptWriter();
        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();

        private Dictionary<string, bool> _moduleExpandedState;
        private ModuleRegistry _registry;
        private ED_CodeGenerator _codeGenSettings;
        private ModuleKind _selectedModuleKind;
        private string _parentModulePath;
        private string _selectedModuleName = string.Empty;

        private enum GenerationState
        {
            Idle = 0,
            InProgress = 1,
            Completed = 2
        }

        private void OnEnable()
        {
            _moduleExpandedState = new Dictionary<string, bool>();
            _parentModulePath = string.Empty;
            ED_CodeGenerator.CreateConfig();
            _codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);

            if (_codeGenSettings == null)
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");

            _generationState = GenerationState.Idle;
            _registry = new ModuleRegistryFactory().FromProject();
        }

        private void OnGUI()
        {
            _bar.DrawWindow("Create Function", "FlowIoC", "Work a Command calls from inside a step",
                null, null, "Creating a Module");

            _body.Begin(this);

            EditorGUILayout.LabelField(FUNCTION_NAME_LABEL, GUILayout.Width(100));
            _functionName = EditorGUILayout.TextField(_functionName);

            if (!string.IsNullOrEmpty(_functionName))
                EditorGUILayout.LabelField($"{_functionName}Function : {BaseTypePreview()}", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            DisplayKindSection();
            DisplayParametersSection();
            DisplayInjectablesSection();
            DisplayParentModuleSelection();

            _body.End();

            DisplayCreateFunctionButton();
        }

        /// <summary>
        /// The base type the pick would produce, spelled out while it is still being picked. It is
        /// the one thing a reader cannot work out from the fields on their own.
        /// </summary>
        private string BaseTypePreview() => _writer.BaseTypeFor(BuildRequest("Preview"));

        private void DisplayKindSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Kind", GUILayout.Width(80));
            _kind = (FunctionKind) EditorGUILayout.EnumPopup(_kind);
            EditorGUILayout.EndHorizontal();

            if (_kind == FunctionKind.Void) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_kind == FunctionKind.Async ? "Callback Type" : "Return Type", GUILayout.Width(80));
            _returnType = EditorGUILayout.TextField(_returnType);
            EditorGUILayout.EndHorizontal();

            if (_kind == FunctionKind.Async)
            {
                EditorGUILayout.HelpBox(
                    "An async function's Execute takes no parameters and returns IEnumerator. It answers through " +
                    "FunctionCompletedCallback, and an empty Callback Type gives the arity that carries no value.",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// The parameters of Execute, up to the four the shipped arities go to. An async function
        /// has none at all, so the section is not drawn for it rather than drawn and ignored.
        /// </summary>
        private void DisplayParametersSection()
        {
            if (_kind == FunctionKind.Async) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(PARAMETERS_LABEL, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_parameters.Count >= FunctionScriptWriter.MAX_PARAMETERS);
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button(ADD_PARAMETER_BUTTON))
                _parameters.Add(new FunctionParameter {Type = "int", Name = "value"});

            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // Noted here and dropped once the list has been drawn: leaving the loop from inside a
            // row ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _parameters.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _parameters[i].Type = EditorGUILayout.TextField(_parameters[i].Type);
                _parameters[i].Name = EditorGUILayout.TextField(_parameters[i].Name);

                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("-", GUILayout.Width(30)))
                    removeAt = i;

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
                _parameters.RemoveAt(removeAt);
        }

        private void DisplayInjectablesSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(INJECTABLES_LABEL, EditorStyles.boldLabel);

            GUI.backgroundColor = Color.green;

            if (GUILayout.Button(ADD_INJECTABLE_BUTTON))
                _injectableNames.Add("NewInjectable");

            GUI.backgroundColor = Color.white;

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

            if (removeAt >= 0)
                _injectableNames.RemoveAt(removeAt);
        }

        /// <summary>
        /// The module the function lands in, drawn the way Create Command asks the same question.
        /// Any kind of module may hold one, because any kind of module may hold a Command.
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

            ModuleHierarchyDrawer.DrawModuleHierarchy(_registry, MODULES_PATH, 0, ref _moduleExpandedState, ref _parentModulePath,
                ref _selectedModuleName, _ => true);

            EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(_parentModulePath))
                EditorGUILayout.LabelField($"Selected: {Path.GetFileName(_parentModulePath)}", EditorStyles.boldLabel);
        }

        private void DisplayCreateFunctionButton()
        {
            bool inProgress = _generationState == GenerationState.InProgress;
            Color background = inProgress ? BUTTON_COLOR_IN_PROGRESS : new ModulePanelTheme().Action;

            if (!_body.FooterButton(this, CREATE_FUNCTION_BUTTON, inProgress || string.IsNullOrEmpty(_parentModulePath), background))
                return;

            if (string.IsNullOrEmpty(_functionName))
            {
                EditorUtility.DisplayDialog(INVALID_FUNCTION_NAME_TITLE, INVALID_FUNCTION_NAME_MESSAGE, "OK");
                return;
            }

            _generationState = GenerationState.InProgress;
            WriteFunction();
        }

        private void WriteFunction()
        {
            string baseModulePath = string.IsNullOrEmpty(_parentModulePath)
                ? Path.Combine(Application.dataPath, MODULES_PATH)
                : _parentModulePath;

            if (!baseModulePath.EndsWith($"{_selectedModuleName}Module", StringComparison.OrdinalIgnoreCase))
                baseModulePath = Path.Combine(baseModulePath, $"{_selectedModuleName}Module");

            string subDirectory = _selectedModuleKind switch
            {
                ModuleKind.Sub => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.SubModules],
                ModuleKind.Test => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.TestModules],
                ModuleKind.Screen => _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.ScreenModules],
                _ => string.Empty
            };

            string modulePath = string.IsNullOrEmpty(subDirectory) ? baseModulePath : Path.Combine(baseModulePath, subDirectory);

            string controllersFolder = _codeGenSettings.DirectoryStructureConfigMap[FolderEVO.FolderType.Controllers];
            string functionPath = _configProvider.ConfigFor(_selectedModuleKind)
                .FindFullFolderPathByID(FolderEVO.FolderType.Controllers, modulePath);

            FunctionScriptRequest request = BuildRequest(_functionName + "Function");
            request.Namespace = NamespaceUtility.GetModuleNamespace(modulePath) + "." + controllersFolder;

            CodeGeneratorUtils.CreateFunction(request, _injectableNames, functionPath);

            _generationState = GenerationState.Completed;
        }

        private FunctionScriptRequest BuildRequest(string className)
        {
            return new FunctionScriptRequest
            {
                ClassName = className,
                Kind = _kind,
                ReturnType = _kind == FunctionKind.Void ? string.Empty : _returnType,
                Parameters = _parameters
            };
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (_generationState == GenerationState.InProgress)
                _generationState = GenerationState.Completed;
        }
    }
}
#endif