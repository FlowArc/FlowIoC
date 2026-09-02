#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private void OnEnable()
        {
            _root = target as RootBase;

            // Rebuilt here rather than kept alive, so a recompile cannot serve a stale declaration.
            _declarations = new ScreenSubContextDeclarations();
            _seed = new ScreenOverrideSeed();
            _summary = new ScreenOverrideSummary();

            // The fold state itself lives in SessionState, so rebuilding this loses nothing.
            _foldouts = new SubContextFoldouts();
        }

        public override void OnInspectorGUI()
        {
            DrawCustomHeader();

            base.OnInspectorGUI();

            GUILayout.Space(10);

            GUIDisableScript(); //TODO
            GUI_InitializeOrder();
            GUI_BindingOptions();
            GUI_InitializeOptions();
            GUI_SetupOptions();
            GUI_LaunchOptions();
            GUI_TestBindingOptions();

            GUI_RootStatus();

            GUI_SubContexts();
        }

        private void DrawCustomHeader()
        {
            if (target == null) return;

            var targetType = target.GetType();
            var titleAttributes = targetType.GetCustomAttributes(typeof(CustomClassHeaderAttribute), true);

            if (titleAttributes.Length > 0)
            {
                var attribute = titleAttributes[0] as CustomClassHeaderAttribute;
                if (attribute != null)
                {
                    GUILayout.Space(10);

                    Rect rect = GUILayoutUtility.GetRect(Screen.width, attribute.Height, GUILayout.ExpandWidth(true));

                    if (Event.current.type == EventType.Repaint)
                    {
                        var gradientRect = new Rect(rect.x, rect.y, rect.width, rect.height);

                        var backgroundColor = GUI.backgroundColor;

                        Texture2D gradientTexture = new Texture2D(1, 1);
                        Color startColor = attribute.StartColor;
                        Color endColor = attribute.EndColor;

                        var gradientStyle = new GUIStyle();
                        gradientStyle.normal.background = DrawGradient((int) rect.width, (int) rect.height, startColor, endColor);
                        GUI.Label(gradientRect, "", gradientStyle);

                        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                        headerStyle.fontSize = attribute.FontSize;
                        headerStyle.alignment = TextAnchor.MiddleLeft;
                        headerStyle.normal.textColor = Color.white;
                        headerStyle.padding.left = 10;

                        string titleText = attribute.Title.ToUpper();
                        GUI.Label(gradientRect, titleText, headerStyle);

                        GUI.backgroundColor = backgroundColor;
                    }

                    GUILayout.Space(5);

                    if (!string.IsNullOrEmpty(attribute.Description))
                    {
                        var descriptionStyle = new GUIStyle(EditorStyles.label);
                        descriptionStyle.wordWrap = true;
                        descriptionStyle.padding.left = 10;
                        descriptionStyle.padding.right = 10;

                        EditorGUILayout.LabelField(attribute.Description, descriptionStyle);

                        GUILayout.Space(5);
                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    }

                    GUILayout.Space(5);
                }
            }
        }

        private Texture2D DrawGradient(int width, int height, Color startColor, Color endColor)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                float gradientPos = (float) x / width;
                Color gradientColor = Color.Lerp(startColor, endColor, gradientPos);

                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = gradientColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private void GUIDisableScript()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        private void GUI_InitializeOrder()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical("box");

            var initializeOrder = EditorGUILayout.IntField("Initialize Order: ", _root.initializeOrder);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "initialize-order");
                _root.initializeOrder = initializeOrder;
                if (!Application.isPlaying)
                    MarkDirty();
            }

            EditorGUILayout.EndVertical();
        }

        private void GUI_BindingOptions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();

            #region Injection

            EditorGUILayout.BeginHorizontal();

            var injectionBinding = EditorGUILayout.ToggleLeft("Bind Injections", _root.AutoBindInjections, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.injectionsBound);

            if (GUI.enabled && !_root.injectionsBound)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            var injectionButton = GUILayout.Button("Bind Injections");
            if (injectionButton)
                _root.BindInjections(true);

            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            #endregion

            #region Mediation

            EditorGUILayout.BeginHorizontal();

            var mediationBinding = EditorGUILayout.ToggleLeft("Bind Mediations", _root.AutoBindMediations, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.mediationsBound);

            if (GUI.enabled && !_root.mediationsBound)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            var mediationButton = GUILayout.Button("Bind Mediations");
            if (mediationButton)
                _root.BindMediations(true);

            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            #endregion

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "binding-flags");
                _root.AutoBindInjections = injectionBinding;
                _root.AutoBindInjections = mediationBinding;

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorGUILayout.EndVertical();
        }

        private void GUI_InitializeOptions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var autoInitialize = EditorGUILayout.ToggleLeft("Auto Initialize", _root.AutoInitialize, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.hasInitialized);

            if (GUI.enabled && !_root.hasInitialized)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            var launchButton = GUILayout.Button("Initialize");
            if (launchButton)
                _root.StartContext(true);

            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "auto-initialize");
                _root.AutoInitialize = autoInitialize;

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GUI_SetupOptions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var autoSetup = EditorGUILayout.ToggleLeft("Auto Setup", _root.AutoSetup, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.hasSetuped && _root.hasInitialized);

            if (GUI.enabled && !_root.hasSetuped)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            var setupButton = GUILayout.Button("Setup");
            if (setupButton)
                _root.Setup(true);

            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "auto-setup");
                _root.AutoSetup = autoSetup;

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GUI_LaunchOptions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var autoLaunch = EditorGUILayout.ToggleLeft("Auto Launch", _root.AutoLaunch, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.hasLaunched && _root.hasInitialized);

            if (GUI.enabled && !_root.hasLaunched)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            var launchButton = GUILayout.Button("Launch");
            if (launchButton)
                _root.Launch(true);

            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "auto-launch");
                _root.AutoLaunch = autoLaunch;

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GUI_TestBindingOptions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var isTest = EditorGUILayout.ToggleLeft("IsTest", _root.IsTest, GUILayout.Width(125));
            GUI.enabled = (Application.isPlaying && !_root.IsTest);

            if (GUI.enabled && !_root.hasLaunched)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = new Color(1, .3f, .4f);

            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_root, "isTest");
                _root.IsTest = isTest;

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void GUI_RootStatus()
        {
            var guiStyle = new GUIStyle(EditorStyles.textField);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Context Status:");

            guiStyle.normal.textColor = _root.injectionsBound ? Color.green : new Color(1, .3f, .4f);
            EditorGUILayout.LabelField("Injections Bound: " + _root.injectionsBound, guiStyle);

            guiStyle.normal.textColor = _root.mediationsBound ? Color.green : new Color(1, .3f, .4f);
            EditorGUILayout.LabelField("Mediations Bound: " + _root.mediationsBound, guiStyle);

            EditorGUILayout.Separator();

            guiStyle.normal.textColor = _root.hasInitialized ? Color.green : new Color(1, .3f, .4f);
            EditorGUILayout.LabelField("Has Initialized: " + _root.hasInitialized, guiStyle);

            guiStyle.normal.textColor = _root.hasLaunched ? Color.green : new Color(1, .3f, .4f);
            EditorGUILayout.LabelField("Has Launched: " + _root.hasLaunched, guiStyle);

            EditorGUILayout.EndVertical();
        }

        private void GUI_SubContexts()
        {
            if (_root.SubContextTypes == null)
                _root.SubContextTypes = new List<SubContextData>();

            if (_root.SubContextTypes.Count != 0)
            {
                EditorGUILayout.BeginVertical("box");

                for (var ii = 0; ii < _root.SubContextTypes.Count; ii++)
                {
                    EditorGUILayout.BeginVertical("box");

                    SubContextData contextData = _root.SubContextTypes[ii];
                    int rootId = _root.GetInstanceID();
                    bool wasExpanded = _foldouts.IsEntryExpanded(rootId, contextData.ContextFullName);

                    EditorGUILayout.BeginHorizontal();

                    bool expanded = EditorGUILayout.Foldout(wasExpanded, new GUIContent(HeaderFor(contextData)), true);

                    if (expanded != wasExpanded)
                        _foldouts.SetEntryExpanded(rootId, contextData.ContextFullName, expanded);

                    GUI_SubContextKind(contextData);

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        _root.SubContextTypes.RemoveAt(ii);
                        MarkDirty();

                        // The layout groups this loop opened have to be closed before leaving, or
                        // the rest of the inspector draws inside them.
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndVertical();
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
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Sub Context"))
            {
                AddSubContextWindow.ShowWindow(_root);
            }
        }

        /// <summary>
        /// The badge that separates a screen context from every other kind of sub-context in a
        /// folded list, drawn the way the Publish window marks a private module. The label is
        /// always laid out, tinted clear when it does not apply, so the remove button beside it
        /// keeps the same place on every row.
        /// </summary>
        private void GUI_SubContextKind(SubContextData contextData)
        {
            Type contextType = _declarations.ResolveType(contextData.ContextFullName);

            Color previous = GUI.color;
            GUI.color = _declarations.IsScreenContext(contextType) ? new Color(1f, 0.8f, 0.3f) : Color.clear;
            EditorGUILayout.LabelField("SCREEN", EditorStyles.miniBoldLabel, GUILayout.Width(50));
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

        private void MarkDirty()
        {
            var prefabScene = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabScene != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabScene.scene);
            }
            else if (PrefabUtility.IsOutermostPrefabInstanceRoot(_root.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(_root);
            }
            else
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif