#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Help;
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

        /// <summary>The air between the two halves of a row, the same the name row keeps.</summary>
        private const float COLUMNS_SPACING = 8f;

        private const string FUNCTION_NAME_LABEL = "Function Name: ";
        private const string CREATE_FUNCTION_BUTTON = "Create Function";
        private const string ADD_PARAMETER_BUTTON = "Add Parameter";
        private const string ADD_INJECTABLE_BUTTON = "Add Injectable";
        private const string PARAMETERS_LABEL = "Execute Parameters:";
        private const string INJECTABLES_LABEL = "Injectables:";
        private const string PARENT_MODULE_LABEL = "Parent Module:";
        private const string USAGE_LABEL = "How it is called:";
        private const string USAGE_CAPTION = "From a Command, or from another Function";
        private const string INVALID_FUNCTION_NAME_TITLE = "Invalid Function Name";
        private const string INVALID_FUNCTION_NAME_MESSAGE = "Please enter a valid Function name.";

        private static readonly Color BUTTON_COLOR_IN_PROGRESS = Color.gray;

        private string _functionName = string.Empty;
        private FunctionKind _kind;
        private string _returnType = "double";
        private static GenerationState _generationState;

        private readonly List<FunctionParameter> _parameters = new List<FunctionParameter>();
        private readonly List<string> _injectableNames = new List<string>();
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly FunctionScriptWriter _writer = new FunctionScriptWriter();
        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();
        private readonly GeneratorNamePreview _preview = new GeneratorNamePreview();
        private readonly GeneratorListBar _listBar = new GeneratorListBar();

        private HelpPainter _painter;
        private InjectableTypeIndex _typeIndex;
        private ModulePicker _picker;
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
            _parentModulePath = string.Empty;
            ED_CodeGenerator.CreateConfig();
            _codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);

            if (_codeGenSettings == null)
                Debug.LogError($"ED_CodeGenerator asset not found. Please ensure it exists at {CodeGeneratorStrings.CONFIG_PATH}.");

            _generationState = GenerationState.Idle;
            _registry = new ModuleRegistryFactory().FromProject();
            _picker = new ModulePicker(_registry);

            // Without this the window is sent no MouseMove events at all, and a row of the picker
            // would only light up when something else happened to repaint it.
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            _bar.DrawWindow("Create Function", "FlowIoC", "Work a Command calls from inside a step",
                null, null, "Create Function");

            _body.Begin(this);

            _functionName = _preview.Draw(
                FUNCTION_NAME_LABEL, _functionName, _functionName + "Function", "Base Type:", BaseTypePreview());

            EditorGUILayout.Space(10);

            DisplayKindSection();
            DisplayUsagePreview();
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

        /// <summary>
        /// The kind on the left and, on the right, the one field the kind asks for - a return
        /// type, or the type an async callback carries - on the same line, in the two columns
        /// the name and its preview stand in above. A void function asks for nothing, and the
        /// right half stays empty rather than closing up, so the kind does not slide about.
        /// </summary>
        private void DisplayKindSection()
        {
            float half = Mathf.Round((EditorGUIUtility.currentViewWidth - COLUMNS_SPACING) * 0.5f);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(half));
            GUILayout.Label("Kind", GUILayout.Width(80));
            _kind = (FunctionKind) EditorGUILayout.EnumPopup(_kind);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginHorizontal();

            if (_kind != FunctionKind.Void)
            {
                GUILayout.Label(_kind == FunctionKind.Async ? "Callback Type" : "Return Type", GUILayout.Width(90));
                _returnType = EditorGUILayout.TextField(_returnType);
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// The parameters of Execute, up to the four the shipped arities go to. An async function
        /// has none at all, so the section gives way to the warning that says so - drawn where
        /// the list would have been, so the reader looking for it finds the reason instead.
        /// </summary>
        private void DisplayParametersSection()
        {
            if (_kind == FunctionKind.Async)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "An async function's Execute takes no parameters and returns IEnumerator. It answers through "
                    + "FunctionCompletedCallback, and an empty Callback Type gives the arity that carries no value.",
                    MessageType.Warning);

                return;
            }

            if (_listBar.Draw(PARAMETERS_LABEL, ADD_PARAMETER_BUTTON, _parameters.Count < FunctionScriptWriter.MAX_PARAMETERS))
                _parameters.Add(new FunctionParameter {Type = "int", Name = "value"});

            // Noted here and dropped once the list has been drawn: leaving the loop from inside a
            // row ends the frame with that row's horizontal group still open, and IMGUI reports an
            // invalid layout state for every repaint after it.
            int removeAt = -1;

            for (int i = 0; i < _parameters.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _parameters[i].Type = EditorGUILayout.TextField(_parameters[i].Type);
                _parameters[i].Name = EditorGUILayout.TextField(_parameters[i].Name);

                if (_listBar.DrawRemove())
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
                _parameters.RemoveAt(removeAt);
        }

        private void DisplayInjectablesSection()
        {
            if (_listBar.Draw(INJECTABLES_LABEL, ADD_INJECTABLE_BUTTON))
                _injectableNames.Add("NewInjectable");

            int removeAt = -1;

            for (int i = 0; i < _injectableNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _injectableNames[i] = EditorGUILayout.TextField(_injectableNames[i]);

                DisplayInjectableStatus(_injectableNames[i]);

                if (_listBar.DrawRemove())
                    removeAt = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
                _injectableNames.RemoveAt(removeAt);
        }

        /// <summary>
        /// Whether the project has a type of that name, said beside the row rather than after the
        /// file is written. A row it cannot place is still written into the function - a member
        /// that is there and does not compile is a report, and the silence it replaced was not -
        /// but the author would rather fix the name here than read the file to find out.
        ///
        /// The lookup is an index built once rather than a search, because this runs per row on
        /// every repaint.
        /// </summary>
        private void DisplayInjectableStatus(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return;

            _typeIndex ??= new InjectableTypeIndex();

            if (_typeIndex.Knows(typeName))
            {
                GUILayout.Label(GUIContent.none, GUILayout.Width(20));
                return;
            }

            var warning = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml"))
            {
                tooltip = $"No type named {typeName.Trim()} is in the project. The member is written either way, "
                          + "so the compiler will name the line - but check the spelling first."
            };

            GUILayout.Label(warning, GUILayout.Width(20));
        }

        /// <summary>
        /// How a Command calls the function, drawn as the Help window draws a snippet - the same
        /// colouring, and the same Copy button beside it.
        ///
        /// It is here because the generated file is only half of what an author needs. A function
        /// says nothing about where it is called from, which is the whole difference between one
        /// and a Command, so the call is the part nobody can read off the class - and it changes
        /// with every field above it: the kind decides the terminator, the parameters become the
        /// arguments, the callback type decides whether there is a callback at all.
        /// </summary>
        private void DisplayUsagePreview()
        {
            if (string.IsNullOrEmpty(_functionName)) return;

            _painter ??= new HelpPainter(new HelpTheme());

            PanelHeader(USAGE_LABEL);

            _painter.Code(_writer.UsageFor(BuildRequest(_functionName + "Function")), USAGE_CAPTION);
        }

        /// <summary>
        /// The bar a panel in this window wears: the Root's purple, an icon, and the panel's name.
        /// The two panels here - the call and the module list - carry the same one, so the window
        /// reads as two things asked rather than as a run of fields.
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
        /// The module the function lands in, drawn the way Create Command asks the same question.
        /// Any kind of module may hold one, because any kind of module may hold a Command.
        /// </summary>
        private void DisplayParentModuleSelection()
        {
            _picker.DrawBar(PARENT_MODULE_LABEL, _parentModulePath);

            EditorGUILayout.BeginVertical();

            _picker.Draw(ref _parentModulePath, ref _selectedModuleName, _ => true, false);
            _selectedModuleKind = _picker.PickedKind;

            EditorGUILayout.EndVertical();
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

            // The pick is the module itself, whatever kind it is - a test module's own folder, not
            // the module it tests - so the file goes into the folder the pick names.
            string modulePath = baseModulePath;

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
                Parameters = _parameters,
                IsTest = _selectedModuleKind == ModuleKind.Test
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