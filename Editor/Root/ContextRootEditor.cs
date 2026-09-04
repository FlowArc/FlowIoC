#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Inspector;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Root
{
    [CustomEditor(typeof(RootBase), true)]
    public class ContextRootEditor : UnityEditor.Editor
    {
        private RootBase _root;

        private ScreenSubContextDeclarations _declarations;
        private ScreenOverrideSeed _seed;
        private ScreenOverrideSummary _summary;
        private SubContextFoldouts _foldouts;
        private RootDirtyMarker _dirtyMarker;

        private FlowPalette _palette;
        private FlowRoleResolver _roles;
        private FlowHelpSource _help;
        private FlowHelpState _helpState;
        private FlowHeaderBar _bar;
        private FlowInspectorGUI _gui;

        /// <summary>
        /// The rows the bar's help button opens and closes together. They are the fields of
        /// RootBase, which is where the lifecycle lives whatever the Root is called.
        /// </summary>
        private static readonly string[] LifecycleMembers =
        {
            nameof(RootBase.initializeOrder),
            nameof(RootBase.AutoBindInjections),
            nameof(RootBase.AutoBindMediations),
            nameof(RootBase.AutoInitialize),
            nameof(RootBase.AutoSetup),
            nameof(RootBase.AutoLaunch),
            nameof(RootBase.IsTest)
        };

        private void OnEnable()
        {
            _root = target as RootBase;

            // Rebuilt here rather than kept alive, so a recompile cannot serve a stale declaration.
            _declarations = new ScreenSubContextDeclarations();
            _seed = new ScreenOverrideSeed();
            _summary = new ScreenOverrideSummary();

            // The fold state itself lives in SessionState, so rebuilding this loses nothing.
            _foldouts = new SubContextFoldouts();

            _dirtyMarker = new RootDirtyMarker();

            _palette = new FlowPalette();
            _roles = new FlowRoleResolver();
            _help = new FlowHelpSource(new MonoScriptText());
            _helpState = new FlowHelpState();
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
            _gui = new FlowInspectorGUI(_palette, _roles, _help, _helpState);
        }

        /// <summary>
        /// Only while the game runs: the lifecycle badges read fields that change without the
        /// inspector being touched, and a repaint is the only way they can say so.
        /// </summary>
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawFlowHeader();

            GUIDisableScript();

            GUI_Lifecycle();
            GUI_SubContexts();
        }

        /// <summary>The role the bar and the card action wear. A Root that resolves to nothing is still a Root.</summary>
        private FlowRole RoleOf()
        {
            return _roles.TryResolve(_root.GetType(), out FlowRole role) ? role : FlowRole.Root;
        }

        private void DrawFlowHeader()
        {
            Type type = _root.GetType();
            FlowRole role = RoleOf();

            bool open = _helpState.IsOpen(type, FlowHelpParser.TypeKey);

            _bar.Draw(role, _roles.TitleFor(type), type.Assembly.GetName().Name, _roles.LabelFor(type, role),
                _help.Summary(type), open,
                () =>
                {
                    bool next = !open;
                    _helpState.SetOpen(type, FlowHelpParser.TypeKey, next);
                    _helpState.SetAll(type, LifecycleMembers, next);
                });
        }

        private void GUIDisableScript()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Every phase of the Root's life in one table: whether it happens on its own, whether it
        /// has happened, and - while the game runs - a way to make it happen now. The separate
        /// status box this replaces said the same things a second time.
        /// </summary>
        private void GUI_Lifecycle()
        {
            Type type = _root.GetType();

            _gui.BeginCard("LIFECYCLE");

            EditorGUI.BeginChangeCheck();

            int order = _gui.IntField(type, nameof(RootBase.initializeOrder), "Initialize Order", _root.initializeOrder);

            bool injections = _gui.Phase(type, nameof(RootBase.AutoBindInjections), "Bind Injections",
                _root.AutoBindInjections, _root.injectionsBound,
                Application.isPlaying && !_root.injectionsBound, out bool runInjections);

            bool mediations = _gui.Phase(type, nameof(RootBase.AutoBindMediations), "Bind Mediations",
                _root.AutoBindMediations, _root.mediationsBound,
                Application.isPlaying && !_root.mediationsBound, out bool runMediations);

            bool initialize = _gui.Phase(type, nameof(RootBase.AutoInitialize), "Initialize",
                _root.AutoInitialize, _root.hasInitialized,
                Application.isPlaying && !_root.hasInitialized, out bool runInitialize);

            bool setup = _gui.Phase(type, nameof(RootBase.AutoSetup), "Setup",
                _root.AutoSetup, _root.hasSetuped,
                Application.isPlaying && _root.hasInitialized && !_root.hasSetuped, out bool runSetup);

            bool launch = _gui.Phase(type, nameof(RootBase.AutoLaunch), "Launch",
                _root.AutoLaunch, _root.hasLaunched,
                Application.isPlaying && _root.hasInitialized && !_root.hasLaunched, out bool runLaunch);

            bool isTest = _gui.Toggle(type, nameof(RootBase.IsTest), "Is Test", _root.IsTest);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "root-lifecycle");

                _root.initializeOrder = order;
                _root.AutoBindInjections = injections;
                _root.AutoBindMediations = mediations;
                _root.AutoInitialize = initialize;
                _root.AutoSetup = setup;
                _root.AutoLaunch = launch;
                _root.IsTest = isTest;

                if (!Application.isPlaying)
                    MarkDirty();
            }

            if (runInjections)
                _root.BindInjections(true);

            if (runMediations)
                _root.BindMediations(true);

            if (runInitialize)
                _root.StartContext(true);

            if (runSetup)
                _root.Setup(true);

            if (runLaunch)
                _root.Launch(true);

            _gui.EndCard();
        }

        private void GUI_SubContexts()
        {
            if (_root.SubContextTypes == null)
                _root.SubContextTypes = new List<SubContextData>();

            if (_root.SubContextTypes.Count != 0)
            {
                _gui.BeginCard("SUB CONTEXTS", true);

                for (var ii = 0; ii < _root.SubContextTypes.Count; ii++)
                {
                    EditorGUILayout.BeginVertical(_gui.CardEntry);

                    SubContextData contextData = _root.SubContextTypes[ii];
                    int rootId = _root.GetInstanceID();
                    bool wasExpanded = _foldouts.IsEntryExpanded(rootId, contextData.ContextFullName);

                    EditorGUILayout.BeginHorizontal();

                    bool expanded = EditorGUILayout.Foldout(wasExpanded, new GUIContent(HeaderFor(contextData)), true);

                    if (expanded != wasExpanded)
                        _foldouts.SetEntryExpanded(rootId, contextData.ContextFullName, expanded);

                    GUI_SubContextKind(contextData);

                    if (GUILayout.Button("-", _gui.EntryAction, GUILayout.Width(24)))
                    {
                        // Recorded, because a mis-click here used to destroy an entry with no way back.
                        Undo.RecordObject(_root, "remove-sub-context");
                        _root.SubContextTypes.RemoveAt(ii);
                        MarkDirty();

                        // The layout groups this loop opened have to be closed before leaving, or
                        // the rest of the inspector draws inside them.
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        _gui.EndCard();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (expanded)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUI.BeginChangeCheck();

                        contextData.AutoSetup =
                            EditorGUILayout.Toggle(new GUIContent("AutoSetup"), contextData.AutoSetup);

                        contextData.IsTest =
                            EditorGUILayout.Toggle(new GUIContent("IsTest"), contextData.IsTest);

                        if (EditorGUI.EndChangeCheck())
                            WriteSubContext(ii, contextData);

                        GUI_ScreenOverride(ii, contextData);

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                _gui.EndCard();
            }

            if (_gui.AccentButton(RoleOf(), "Add Sub Context"))
                AddSubContextWindow.ShowWindow(_root, RoleOf());
        }

        /// <summary>
        /// The badge that says what kind of sub-context a folded entry is: a screen in the screen
        /// colour, a connector in the connector's. A sub-context is not a component, so this list
        /// is the only place those two colours are ever seen. The badge is always laid out, blank
        /// when it says nothing, so the remove button beside it keeps the same place on every row.
        /// </summary>
        private void GUI_SubContextKind(SubContextData contextData)
        {
            Type contextType = _declarations.ResolveType(contextData.ContextFullName);

            string badge = string.Empty;
            Color color = Color.clear;

            if (_declarations.IsScreenContext(contextType))
            {
                badge = "SCREEN";
                color = _palette.Accent(FlowRole.Screen, EditorGUIUtility.isProSkin);
            }
            else if (contextData.ContextName != null && contextData.ContextName.Contains("Connector"))
            {
                badge = "CONNECTOR";
                color = _palette.Accent(FlowRole.Connector, EditorGUIUtility.isProSkin);
            }

            Color previous = GUI.color;
            GUI.color = color;

            EditorGUILayout.LabelField(badge, EditorStyles.miniBoldLabel, GUILayout.Width(68));

            GUI.color = previous;
        }

        /// <summary>
        /// The header a folded entry shows: the context's name, and the override's manager and
        /// layer when the Root deviates from what the context declares.
        /// </summary>
        private string HeaderFor(SubContextData contextData)
        {
            string summary = _summary.For(contextData);

            return string.IsNullOrEmpty(summary)
                ? contextData.ContextName
                : $"{contextData.ContextName}  ({summary})";
        }

        /// <summary>
        /// A screen context's entry says how that screen is configured, and lets the Root override
        /// it. With the override off the values come from the context's own code and are shown
        /// read-only, so a Root always tells the truth about the screens it lists. It folds on its
        /// own, because a Root listing several screens is otherwise a wall of fields.
        /// </summary>
        private void GUI_ScreenOverride(int index, SubContextData contextData)
        {
            Type contextType = _declarations.ResolveType(contextData.ContextFullName);
            if (!_declarations.IsScreenContext(contextType))
                return;

            int rootId = _root.GetInstanceID();
            string summary = _summary.For(contextData);
            string label = string.IsNullOrEmpty(summary) ? "Screen" : $"Screen  ({summary})";

            bool wasExpanded = _foldouts.IsScreenExpanded(rootId, contextData.ContextFullName);
            bool expanded = EditorGUILayout.Foldout(wasExpanded, new GUIContent(label), true);

            if (expanded != wasExpanded)
                _foldouts.SetScreenExpanded(rootId, contextData.ContextFullName, expanded);

            if (!expanded)
                return;

            EditorGUI.indentLevel++;
            GUI_ScreenOverrideBody(index, contextData, contextType);
            EditorGUI.indentLevel--;
        }

        private void GUI_ScreenOverrideBody(int index, SubContextData contextData, Type contextType)
        {
            bool read = _declarations.TryRead(contextType, out ScreenCVO declaration, out string error);

            if (read)
                EditorGUILayout.LabelField("Load", $"{declaration.Load.Kind}: {declaration.Load.Key}");
            else
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            bool overrideScreen = EditorGUILayout.Toggle(new GUIContent("Override Screen"), contextData.OverrideScreen);

            if (EditorGUI.EndChangeCheck())
            {
                bool turnedOn = overrideScreen && !contextData.OverrideScreen;
                contextData.OverrideScreen = overrideScreen;

                if (turnedOn && read)
                    contextData = _seed.Apply(contextData, declaration);

                WriteSubContext(index, contextData);
            }

            if (!contextData.OverrideScreen)
            {
                if (read)
                    GUI_DeclaredScreenValues(declaration);

                return;
            }

            EditorGUI.BeginChangeCheck();

            contextData.ScreenManagerId = EditorGUILayout.IntField("Manager Id", contextData.ScreenManagerId);
            contextData.ScreenLayer = EditorGUILayout.IntField("Layer", contextData.ScreenLayer);
            contextData.ScreenTag = (ScreenTag) EditorGUILayout.EnumPopup("Tag", contextData.ScreenTag);
            contextData.ScreenHasShowAnimation = EditorGUILayout.Toggle("Has Show Animation", contextData.ScreenHasShowAnimation);
            contextData.ScreenHasHideAnimation = EditorGUILayout.Toggle("Has Hide Animation", contextData.ScreenHasHideAnimation);

            if (EditorGUI.EndChangeCheck())
                WriteSubContext(index, contextData);
        }

        private void GUI_DeclaredScreenValues(ScreenCVO declaration)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Manager Id", declaration.ManagerId);
                EditorGUILayout.IntField("Layer", declaration.Layer);
                EditorGUILayout.EnumPopup("Tag", declaration.Tag);
                EditorGUILayout.Toggle("Has Show Animation", declaration.HasShowAnimation);
                EditorGUILayout.Toggle("Has Hide Animation", declaration.HasHideAnimation);
            }
        }

        private void WriteSubContext(int index, SubContextData contextData)
        {
            Undo.RecordObject(_root, "screen-override");
            _root.SubContextTypes[index] = contextData;

            if (!Application.isPlaying)
                MarkDirty();
        }

        private void MarkDirty() => _dirtyMarker.Mark(_root);
    }
}
#endif