#if UNITY_EDITOR
using System;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.ScreenModule.Data.Editor
{
    [CustomEditor(typeof(CD_Screen))]
    internal class ScreenConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _defaultLayerProp;
        private SerializedProperty _loadTypeProp;
        private SerializedProperty _directPrefabProp;
        private SerializedProperty _screenTagProp;
        private SerializedProperty _resourcePathProp;
        private SerializedProperty _addressableKeyProp;
        private SerializedProperty _hasShowAnimationProp;
        private SerializedProperty _hasHideAnimationProp;
        private SerializedProperty _viewTypeNameProp;
        private SerializedProperty _mediatorTypeNameProp;

        private void OnEnable()
        {
            if (target == null) return;

            InitializeProperties();
        }

        private void InitializeProperties()
        {
            _defaultLayerProp = serializedObject.FindProperty("_defaultLayer");
            _loadTypeProp = serializedObject.FindProperty("_loadType");
            _directPrefabProp = serializedObject.FindProperty("_directPrefab");
            _screenTagProp = serializedObject.FindProperty("_screenTag");
            _resourcePathProp = serializedObject.FindProperty("_resourcePath");
            _addressableKeyProp = serializedObject.FindProperty("_addressableKey");
            _hasShowAnimationProp = serializedObject.FindProperty("_hasShowAnimation");
            _hasHideAnimationProp = serializedObject.FindProperty("_hasHideAnimation");
            _viewTypeNameProp = serializedObject.FindProperty("_viewTypeName");
            _mediatorTypeNameProp = serializedObject.FindProperty("_mediatorTypeName");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("No valid target object.", MessageType.Warning);
                return;
            }

            serializedObject.Update();

            string path = AssetDatabase.GetAssetPath(target);
            bool isPreview = string.IsNullOrEmpty(path);

            if (isPreview)
            {
                EditorGUILayout.HelpBox(
                    "This is an in-memory preview of CD_Screen. Some validations (OnValidate) may not apply.",
                    MessageType.Info
                );

                if (_defaultLayerProp == null)
                {
                    InitializeProperties();
                }
            }

            DrawProperties();

            if (!isPreview)
            {
                bool hasErrors = ValidateConfig();
                if (hasErrors)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "Please fix the errors above before using this config!",
                        MessageType.Error
                    );
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperties()
        {
            if (_defaultLayerProp != null) EditorGUILayout.PropertyField(_defaultLayerProp);
            if (_loadTypeProp != null) EditorGUILayout.PropertyField(_loadTypeProp);

            if (_screenTagProp != null)
            {
                EditorGUILayout.PropertyField(_screenTagProp);
                if (_screenTagProp.enumValueIndex >= 0)
                {
                    ValidateScreentag();
                }
            }

            if (_directPrefabProp != null)
            {
                // EditorGUILayout.PropertyField(_directPrefabProp);
                if (_directPrefabProp.objectReferenceValue != null)
                {
                    ValidatePrefabComponent(_directPrefabProp);
                }
            }

            if (_loadTypeProp != null)
            {
                ScreenLoadType loadType = (ScreenLoadType) _loadTypeProp.enumValueIndex;
                switch (loadType)
                {
                    case ScreenLoadType.Resource:
                        if (_resourcePathProp != null)
                            EditorGUILayout.PropertyField(_resourcePathProp);
                        break;

                    case ScreenLoadType.Addressable:
                        if (_addressableKeyProp != null)
                            EditorGUILayout.PropertyField(_addressableKeyProp);
                        break;
                    case ScreenLoadType.DirectPrefab:
                        if (_directPrefabProp != null)
                            EditorGUILayout.PropertyField(_directPrefabProp);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            string path = AssetDatabase.GetAssetPath(target);
            bool isPreview = string.IsNullOrEmpty(path);
            if (!isPreview)
            {
                // EditorGUI.BeginDisabledGroup(true);
                if (_viewTypeNameProp != null) EditorGUILayout.PropertyField(_viewTypeNameProp);
                if (_mediatorTypeNameProp != null) EditorGUILayout.PropertyField(_mediatorTypeNameProp);
                // EditorGUI.EndDisabledGroup();
            }

            if (_hasShowAnimationProp != null) EditorGUILayout.PropertyField(_hasShowAnimationProp);
            if (_hasHideAnimationProp != null) EditorGUILayout.PropertyField(_hasHideAnimationProp);
        }


        private bool ValidateConfig()
        {
            if (_defaultLayerProp == null || _loadTypeProp == null)
                return true;

            bool hasErrors = _defaultLayerProp.intValue < 0;

            ScreenLoadType loadType = (ScreenLoadType) _loadTypeProp.enumValueIndex;
            switch (loadType)
            {
                case ScreenLoadType.Resource:
                    if (_resourcePathProp == null || string.IsNullOrEmpty(_resourcePathProp.stringValue))
                        hasErrors = true;
                    break;

                case ScreenLoadType.Addressable:
                    if (_addressableKeyProp == null || string.IsNullOrEmpty(_addressableKeyProp.stringValue))
                        hasErrors = true;
                    _directPrefabProp.objectReferenceValue = null;
                    break;
            }

            return hasErrors;
        }

        private void ValidatePrefabComponent(SerializedProperty prefabProp)
        {
            if (prefabProp.objectReferenceValue is not GameObject prefab) return;

            IScreenBody screenBody = prefab.GetComponent<IScreenBody>();
            if (screenBody == null)
            {
                EditorGUILayout.HelpBox(
                    "Prefab must have an IScreenBody component!",
                    MessageType.Error
                );
            }
        }

        private void ValidateScreentag()
        {
            if (_screenTagProp == null)
            {
                EditorGUILayout.HelpBox(
                    "Value have to be attached!",
                    MessageType.Error
                );
            }
        }
    }
}
#endif