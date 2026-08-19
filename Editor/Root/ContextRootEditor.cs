#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
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

        private void OnEnable()
        {
            _root = target as RootBase;
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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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
                GUI.backgroundColor = new Color(1,.3f,.4f);

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

            guiStyle.normal.textColor = _root.injectionsBound ? Color.green : new Color(1,.3f,.4f);
            EditorGUILayout.LabelField("Injections Bound: " + _root.injectionsBound, guiStyle);

            guiStyle.normal.textColor = _root.mediationsBound ? Color.green : new Color(1,.3f,.4f);
            EditorGUILayout.LabelField("Mediations Bound: " + _root.mediationsBound, guiStyle);

            EditorGUILayout.Separator();

            guiStyle.normal.textColor = _root.hasInitialized ? Color.green : new Color(1,.3f,.4f);
            EditorGUILayout.LabelField("Has Initialized: " + _root.hasInitialized, guiStyle);

            guiStyle.normal.textColor = _root.hasLaunched ? Color.green : new Color(1,.3f,.4f);
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
                    EditorGUILayout.BeginHorizontal();

                    var contextData = _root.SubContextTypes[ii];
                    EditorGUILayout.LabelField(contextData.ContextName);

                    if (GUILayout.Button("-"))
                    {
                        _root.SubContextTypes.RemoveAt(ii);
                        MarkDirty();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();

                    contextData.AutoSetup =
                        EditorGUILayout.Toggle(new GUIContent("AutoSetup"), contextData.AutoSetup);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_root, "auto-setup");
                        _root.SubContextTypes[ii] = contextData;

                        if (!Application.isPlaying)
                            MarkDirty();
                    }

                    contextData.IsTest =
                        EditorGUILayout.Toggle(new GUIContent("IsTest"), contextData.IsTest);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_root, "isTest");
                        _root.SubContextTypes[ii] = contextData;

                        if (!Application.isPlaying)
                            MarkDirty();
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