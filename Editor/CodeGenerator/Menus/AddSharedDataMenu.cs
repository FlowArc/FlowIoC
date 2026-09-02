#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// Gives a module that already exists the Shared folder it was not created with.
    ///
    /// Shared is a tick in Create Module, which means a module only gets offered it on the day it
    /// is created - and the need usually arrives later, when a second module turns out to want its
    /// data. This window closes that gap, and because every step of the install checks before it
    /// writes, it doubles as the repair for a Shared folder someone half-deleted.
    /// </summary>
    internal class AddSharedDataMenu : EditorWindow
    {
        private const string MODULES_PATH = "Modules";
        private const string WINDOW_TITLE = "Add Shared Data";
        private const string ADD_BUTTON = "Add Shared Data";
        private const string MODULE_LABEL = "Module:";
        private const float ADD_BUTTON_HEIGHT = 40;

        private static readonly Color BUTTON_COLOR_IDLE = Color.cyan;

        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private readonly ModuleAssetPathResolver _pathResolver = new ModuleAssetPathResolver();

        private ModuleRegistry _registry;
        private Dictionary<string, bool> _moduleExpandedState;
        private string _selectedModulePath;
        private string _selectedModuleName;
        private Vector2 _scrollPosition;
        private string _lastResult;

        [MenuItem("Tools/FlowIoC/Add Shared Data", false, -1295)]
        private static void ShowWindow()
        {
            GetWindow<AddSharedDataMenu>(WINDOW_TITLE);
        }

        private void OnEnable()
        {
            _moduleExpandedState = new Dictionary<string, bool>();
            _selectedModulePath = string.Empty;
            _selectedModuleName = string.Empty;
            _lastResult = string.Empty;
            ED_CodeGenerator.CreateConfig();
            _registry = new ModuleRegistryFactory().FromProject();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "Adds Scripts/Shared to an existing module: the folders, an assembly of its own, its "
                + "namespace settings file, and the references that let the module and its screen, "
                + "sub and test modules read what it holds.\n\n"
                + "Only main and sub modules are offered it - a screen or test module holds nothing "
                + "another module reads.",
                MessageType.Info);

            EditorGUILayout.Space(10);
            DisplayModuleSelection();

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }

            DisplayAddButton();

            EditorGUILayout.EndVertical();
        }

        private void DisplayModuleSelection()
        {
            var labelStyle = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            GUI.backgroundColor = new Color(.6f, .4f, 1f);
            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(33));
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(35), GUILayout.Height(33));
            EditorGUILayout.LabelField(MODULE_LABEL, labelStyle, GUILayout.Height(33));
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(120));
            EditorGUILayout.BeginVertical();
            ModuleHierarchyDrawer.DrawModuleHierarchy(
                _registry, MODULES_PATH, 0, ref _moduleExpandedState, ref _selectedModulePath, ref _selectedModuleName, CanSelect);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_selectedModulePath))
            {
                EditorGUILayout.LabelField($"Selected: {Path.GetFileName(_selectedModulePath)}", EditorStyles.boldLabel);
            }
        }

        /// <summary>
        /// Every layout with a Shared folder in it. A screen module has one now that its public
        /// signal holder lives there; a test module still does not, because it holds nothing
        /// another module reads.
        /// </summary>
        private bool CanSelect(ModuleKind kind)
        {
            return kind == ModuleKind.Main || kind == ModuleKind.Sub || kind == ModuleKind.Screen;
        }

        private void DisplayAddButton()
        {
            GUI.backgroundColor = BUTTON_COLOR_IDLE;
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_selectedModulePath));

            if (GUILayout.Button(ADD_BUTTON, GUILayout.Height(ADD_BUTTON_HEIGHT)))
            {
                AddSharedData();
            }

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }

        private void AddSharedData()
        {
            string moduleAssetPath = _pathResolver.ToAssetPath(_selectedModulePath);

            if (!_registry.TryGetModule(moduleAssetPath, out ModuleDescriptorEVO module))
            {
                _lastResult = $"'{moduleAssetPath}' is not in the module index. Run Module Configuration > Detect & Fix Module Index first.";
                Debug.LogError($"<color=cyan>FlowIoC:</color> Add Shared Data - {_lastResult}");
                return;
            }

            DirectoryStructureConfig config = _configProvider.ConfigFor(module.Kind);
            SharedDataReport report = new SharedDataInstaller().Install(_registry, module, _selectedModulePath, config);

            report.Log(module.Name);
            _lastResult = report.Succeeded ? report.Summary() : report.Error;

            // A new assembly changes what compiles against what, and the index has folder GUIDs to
            // record for the folders that were just laid down.
            if (report.Succeeded && report.ChangedAnything)
            {
                RegisterFolders(module, config);
            }
        }

        private void RegisterFolders(ModuleDescriptorEVO module, DirectoryStructureConfig config)
        {
            var codeGenSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null) return;

            new ModuleIndexRegistrar().Register(_selectedModulePath, config, codeGenSettings.DirectoryStructureConfigMap.Keys);
            _registry = new ModuleRegistryFactory().FromProject();
        }
    }
}
#endif