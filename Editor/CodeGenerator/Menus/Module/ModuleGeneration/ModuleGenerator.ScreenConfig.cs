#if UNITY_EDITOR
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.ScreenModule.Data;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static void CreateScreenConfig(
            string screenConfigPath,
            string moduleName,
            ScreenConfigData screenConfigData,
            GameObject directPrefab
        )
        {
            string relativePath = screenConfigPath.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            if (!Directory.Exists(relativePath))
            {
                Directory.CreateDirectory(relativePath);
            }

            string assetPath = Path.Combine(relativePath, $"CD_{moduleName}.asset").Replace("\\", "/");
            CD_Screen screenConfig = ScriptableObject.CreateInstance<CD_Screen>();
            screenConfig.name = $"CD_{moduleName}";

            if (screenConfigData != null)
            {
                SerializedObject so = new SerializedObject(screenConfig);
                var defaultLayerProp = so.FindProperty("_defaultLayer");
                if (defaultLayerProp != null) defaultLayerProp.intValue = screenConfigData.DefaultLayer;

                var hasShowAnimationProp = so.FindProperty("_hasShowAnimation");
                if (hasShowAnimationProp != null) hasShowAnimationProp.boolValue = screenConfigData.HasOpenAnimation;

                var hasHideAnimationProp = so.FindProperty("_hasHideAnimation");
                if (hasHideAnimationProp != null) hasHideAnimationProp.boolValue = screenConfigData.HasCloseAnimation;

                var loadTypeProp = so.FindProperty("_loadType");
                if (loadTypeProp != null) loadTypeProp.enumValueIndex = (int) screenConfigData.LoadType;

                switch (screenConfigData.LoadType)
                {
                    case ScreenLoadType.DirectPrefab:
                        var directPrefabProp = so.FindProperty("_directPrefab");
                        if (directPrefabProp != null) directPrefabProp.objectReferenceValue = directPrefab;
                        break;
                    case ScreenLoadType.Resource:
                        var resourcePathProp = so.FindProperty("_resourcePath");
                        if (resourcePathProp != null) resourcePathProp.stringValue = screenConfigData.ResourcePath;
                        break;
                    case ScreenLoadType.Addressable:
                        var addressableKeyProp = so.FindProperty("_addressableKey");
                        if (addressableKeyProp != null) addressableKeyProp.stringValue = screenConfigData.AddressableKey;
                        break;
                }

                string viewName = moduleName + "View";
                string mediatorName = moduleName + "Mediator";

                var viewTypeNameProp = so.FindProperty("_viewTypeName");
                if (viewTypeNameProp != null) viewTypeNameProp.stringValue = viewName;

                var mediatorTypeNameProp = so.FindProperty("_mediatorTypeName");
                if (mediatorTypeNameProp != null) mediatorTypeNameProp.stringValue = mediatorName;

                so.ApplyModifiedProperties();
            }

            AssetDatabase.CreateAsset(screenConfig, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorPrefs.SetString(PREF_KEY_SCREEN_CONFIG_PATH, assetPath);
        }
    }
}
#endif