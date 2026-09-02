#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        [DidReloadScripts]
        private static void CodeGenerationCompleted()
        {
            if (!EditorWindow.HasOpenInstances<CreateModule.CreateModuleMenu>())
                return;

            var createModuleWindow = EditorWindow.GetWindow<CreateModule.CreateModuleMenu>();
            createModuleWindow.Focus();

            if (EditorWindow.focusedWindow == null || EditorWindow.focusedWindow.GetType() != typeof(CreateModule.CreateModuleMenu))
                return;

            if (!EditorPrefs.HasKey(MODULE_GENERATION_WORKING))
                return;

            int storedValue = EditorPrefs.GetInt(SELECTED_MODULE_TYPE, 0);
            _selectedModuleType = (ModuleType) storedValue;

            try
            {
                switch (_selectedModuleType)
                {
                    case ModuleType.Main:
                        ModuleTypeMainGenerationComplete();
                        break;
                    case ModuleType.Test:
                        ModuleTypeTestGenerationComplete();
                        break;
                    case ModuleType.Screen:
                        ModuleTypeScreenGenerationComplete();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                ClearPrefs();
                Debug.LogError(e);
            }
        }

        private static void ModuleTypeTestGenerationComplete()
        {
            string moduleName = EditorPrefs.GetString(KEY_MODULE_NAME);
            string rootPrefixName = moduleName.Replace("TestView", "");
            string rootClassName = rootPrefixName + "Root";
            string rootNamespace = EditorPrefs.GetString(KEY_CONTEXT_NAMESPACE);
            string sceneName = EditorPrefs.GetString(KEY_SCREEN_NAME);
            string scenePath = EditorPrefs.GetString(KEY_SCENE_PATH);
            bool createScene = EditorPrefs.GetBool(BOOL_CREATE_SCREEN);

            List<Type> possibleAssemblyFiles = AssemblyHelper.GetAllTypesFromAssemblies(moduleName);

            Type rootType = possibleAssemblyFiles.FirstOrDefault(x => x.Name == rootClassName && x.Namespace == rootNamespace);

            if (createScene)
            {
                GameObject rootGameObject = new GameObject(rootPrefixName + "Root");
                if (rootType != null)
                {
                    rootGameObject.AddComponent(rootType);
                }
                else
                {
                    Debug.LogWarning($"Root script '{rootClassName}' not found in namespace '{rootNamespace}'.");
                }

                string relativeScenePath = scenePath.Replace(Application.dataPath, "Assets") + "/" + sceneName + ".unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), relativeScenePath);
            }

            ClearPrefs();
            AssetDatabase.Refresh();
        }

        private static void ModuleTypeMainGenerationComplete()
        {
            string moduleName = EditorPrefs.GetString(KEY_MODULE_NAME);
            string rootPrefixName = moduleName.Replace("View", "");
            string rootClassName = rootPrefixName + "Root";
            string rootNamespace = EditorPrefs.GetString(KEY_CONTEXT_NAMESPACE);
            string sceneName = EditorPrefs.GetString(KEY_SCREEN_NAME);
            string scenePath = EditorPrefs.GetString(KEY_SCENE_PATH);
            bool createScene = EditorPrefs.GetBool(BOOL_CREATE_SCREEN);

            List<Type> possibleAssemblyFiles = AssemblyHelper.GetAllTypesFromAssemblies(moduleName);

            if (createScene)
            {
                Type rootType = possibleAssemblyFiles.FirstOrDefault(x => x.Name == rootClassName && x.Namespace == rootNamespace);

                GameObject rootGameObject = new GameObject(rootPrefixName + "Root");
                if (rootType != null)
                {
                    rootGameObject.AddComponent(rootType);
                    Debug.LogWarning($"Root gameObject created with script '{rootClassName}' in namespace '{rootNamespace}'.");
                }
                else
                {
                    Debug.LogWarning($"Root script '{rootClassName}' not found in namespace '{rootNamespace}'.");
                }

                string relativeScenePath = scenePath.Replace(Application.dataPath, "Assets") + "/" + sceneName + ".unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), relativeScenePath);
            }

            ClearPrefs();
            AssetDatabase.Refresh();
        }

        private static void ModuleTypeScreenGenerationComplete()
        {
            string screenName = EditorPrefs.GetString(KEY_FILE_NAME) + "View";
            string moduleName = EditorPrefs.GetString(KEY_MODULE_NAME);
            string testModuleName = EditorPrefs.GetString(KEY_MODULE_NAME) + "Test";
            string screenNamespace = EditorPrefs.GetString(KEY_VIEW_NAMESPACE);
            string rootPrefixName = screenName.Replace("ScreenView", "");
            string rootPrefixClassName = screenName.Replace("View", "");
            string rootClassName = rootPrefixClassName + "TestRoot";
            string rootNamespace = EditorPrefs.GetString(KEY_CONTEXT_NAMESPACE);
            string prefabName = screenName.Replace("View", "");
            string sceneName = EditorPrefs.GetString(KEY_SCREEN_NAME);
            string scenePath = EditorPrefs.GetString(KEY_SCENE_PATH);
            string parentFolderName = EditorPrefs.GetString(KEY_PARENT_FOLDER_PATH);
            string screenPrefabPath = EditorPrefs.GetString(SCREEN_PREFAB_PATH);
            string screenContextFullName = EditorPrefs.GetString(KEY_SCREEN_CONTEXT_FULL_NAME);

            ClearPrefs();

            List<Type> possibleAssemblyFiles = AssemblyHelper.GetAllTypesFromAssemblies(moduleName);
            List<Type> possibleAssemblyFilesForTest = AssemblyHelper.GetAllTypesFromAssemblies(testModuleName);

            Type screenType = possibleAssemblyFiles.FirstOrDefault(x => x.Name == screenName && x.Namespace == screenNamespace);
            Type rootType = possibleAssemblyFilesForTest.FirstOrDefault(x => x.Name == rootClassName && x.Namespace == rootNamespace);

            GameObject screenServiceRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CodeGeneratorStrings.SCREEN_SERVICE_ROOT_PATH);
            if (screenServiceRootPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(screenServiceRootPrefab, SceneManager.GetActiveScene());
            }
            else
            {
                Debug.LogError($"ScreenServiceRoot prefab not found at: {CodeGeneratorStrings.SCREEN_SERVICE_ROOT_PATH}");
            }

            GameObject rootGameObject = new GameObject(rootPrefixName + "TestRoot");
            if (rootType != null)
            {
                RootBase testRoot = (RootBase) rootGameObject.AddComponent(rootType);

                // The test Root hosts the screen's real context rather than a copy of its bindings,
                // so the screen is declared once and the test scene exercises that declaration.
                if (!string.IsNullOrEmpty(screenContextFullName))
                {
                    testRoot.SubContextTypes = new List<SubContextData>
                    {
                        new SubContextData
                        {
                            ContextFullName = screenContextFullName,
                            ContextName = screenContextFullName.Substring(screenContextFullName.LastIndexOf('.') + 1),
                            AutoSetup = true,
                            IsTest = false
                        }
                    };
                }
            }
            else
            {
                Debug.LogWarning($"Root script '{rootClassName}' not found in namespace '{rootNamespace}'.");
            }

            string screenManagerPrefabPath = CodeGeneratorStrings.GetPath(CodeGeneratorStrings.SCREEN_MANAGER_PREFAB_PATH, parentFolderName);
            ScreenManager screenManagerPrefab = AssetDatabase.LoadAssetAtPath<ScreenManager>(screenManagerPrefabPath);
            ScreenManager screenManager = (ScreenManager) PrefabUtility.InstantiatePrefab(screenManagerPrefab, rootGameObject.transform);

            GameObject screenGameObject = new GameObject(prefabName, typeof(RectTransform));
            screenGameObject.transform.SetParent(screenManager.ManagerData.ScreenLayerList[0].transform);

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent(new UiInputModuleType().Resolve());

            ViewInjector viewInjector = screenGameObject.AddComponent<ViewInjector>();
            screenGameObject.AddComponent(screenType);

            if (screenGameObject.transform is RectTransform rectTransform)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            viewInjector.InitializeForEditor();

            if (!Directory.Exists(screenPrefabPath))
                Directory.CreateDirectory(screenPrefabPath);

            string finalPrefabPath = screenPrefabPath + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(screenGameObject, finalPrefabPath, InteractionMode.UserAction);

            MakePrefabAddressable(finalPrefabPath, prefabName);

            string relativeScenePath = scenePath.Replace(Application.dataPath, "Assets") + "/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), relativeScenePath);

            AssetDatabase.Refresh();
            Selection.activeGameObject = screenGameObject;

            Debug.Log($"Screen prefab '{prefabName}' has been created and marked as Addressable. Scene saved at: {relativeScenePath}");
        }
    }
}
#endif