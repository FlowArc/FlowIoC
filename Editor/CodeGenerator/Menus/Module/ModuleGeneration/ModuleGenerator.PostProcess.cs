#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.ViewsMediators.Manager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        /// <summary>
        /// The second half of a run: the scripts the first half wrote have compiled, so the Root
        /// can be put into the scene the first half made. It runs whenever a handoff is pending,
        /// whether or not the Create Module window is open or focused - the record is the
        /// instruction, and a run whose second half waited on a window that had lost focus was a
        /// run that never cleared its record.
        /// </summary>
        [DidReloadScripts]
        private static void CodeGenerationCompleted()
        {
            var store = new ModuleGenerationHandoffStore();
            ModuleGenerationHandoffEVO handoff = store.Read();

            if (handoff == null)
                return;

            // Consumed before anything runs, so that a throw below cannot leave the record for the
            // next reload to act on with whatever scene is open by then.
            store.Clear();

            try
            {
                switch (handoff.ModuleType)
                {
                    case ModuleType.Main:
                    case ModuleType.Test:
                        PlaceRootInScene(handoff);
                        break;
                    case ModuleType.Screen:
                        BuildScreenTestScene(handoff);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// A main or a test module's scene: the Root, as a GameObject carrying the compiled Root
        /// component. A run that asked for no scene has nothing to do here.
        /// </summary>
        private static void PlaceRootInScene(ModuleGenerationHandoffEVO handoff)
        {
            if (string.IsNullOrEmpty(handoff.ScenePath))
                return;

            // The Root is named for what it roots, so a System module's is PlayerSystemRoot rather
            // than PlayerRoot. The generator recorded the name it wrote; a run that wrote no Root
            // falls back to the plain name, the way it always resolved.
            string rootClassName = string.IsNullOrEmpty(handoff.RootName)
                ? handoff.ModuleName + "Root"
                : handoff.RootName;

            Type rootType = AssemblyHelper.GetAllTypesFromAssemblies(handoff.ModuleName)
                .FirstOrDefault(x => x.Name == rootClassName && x.Namespace == handoff.ContextNamespace);

            new GeneratedScene(handoff.ScenePath).Edit(scene =>
            {
                GameObject rootGameObject = new GameObject(rootClassName);

                if (rootType != null)
                {
                    rootGameObject.AddComponent(rootType);
                    Debug.Log($"<color=cyan>[FlowIoC]</color> '{rootClassName}' placed in '{scene.path}'.");
                }
                else
                {
                    Debug.LogWarning($"<color=cyan>[FlowIoC]</color> Root script '{rootClassName}' was not found in "
                                     + $"namespace '{handoff.ContextNamespace}', so '{rootGameObject.name}' in '{scene.path}' "
                                     + "carries no Root component yet. Add it by hand once the script compiles.");
                }
            });

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// A screen module's test scene: the two service Roots the screen needs, the test Root
        /// hosting the screen's context, the ScreenManager under it, and the screen itself as a
        /// connected prefab on the first layer. A run that asked for no scene made no prefab
        /// folder either, and has nothing to do here.
        /// </summary>
        private static void BuildScreenTestScene(ModuleGenerationHandoffEVO handoff)
        {
            if (string.IsNullOrEmpty(handoff.ScenePath))
                return;

            string screenName = handoff.ModuleName + "View";
            string prefabName = handoff.ModuleName;
            string rootObjectName = TrimScreen(handoff.ModuleName) + "TestRoot";

            List<Type> possibleAssemblyFiles = AssemblyHelper.GetAllTypesFromAssemblies(handoff.ModuleName);
            List<Type> possibleAssemblyFilesForTest = AssemblyHelper.GetAllTypesFromAssemblies(handoff.ModuleName + "Test");

            Type screenType = possibleAssemblyFiles.FirstOrDefault(x => x.Name == screenName && x.Namespace == handoff.ViewNamespace);
            Type rootType = possibleAssemblyFilesForTest.FirstOrDefault(x => x.Name == handoff.RootName && x.Namespace == handoff.ContextNamespace);

            GameObject screenGameObject = null;

            new GeneratedScene(handoff.ScenePath).Edit(scene =>
            {
                GameObject screenServiceRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CodeGeneratorStrings.SCREEN_SERVICE_ROOT_PATH);
                if (screenServiceRootPrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(screenServiceRootPrefab, scene);
                }
                else
                {
                    Debug.LogError($"ScreenServiceRoot prefab not found at: {CodeGeneratorStrings.SCREEN_SERVICE_ROOT_PATH}");
                }

                // The screen is addressable, and an addressable screen loads through the asset
                // service, so the scene that runs it carries AssetServiceRoot beside
                // ScreenServiceRoot.
                GameObject assetServiceRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CodeGeneratorStrings.ASSET_SERVICE_ROOT_PATH);
                if (assetServiceRootPrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(assetServiceRootPrefab, scene);
                }
                else
                {
                    Debug.LogError($"AssetServiceRoot prefab not found at: {CodeGeneratorStrings.ASSET_SERVICE_ROOT_PATH}");
                }

                GameObject rootGameObject = new GameObject(rootObjectName);
                if (rootType != null)
                {
                    RootBase testRoot = (RootBase) rootGameObject.AddComponent(rootType);

                    // The test Root hosts the screen's real context rather than a copy of its
                    // bindings, so the screen is declared once and the test scene exercises that
                    // declaration.
                    if (!string.IsNullOrEmpty(handoff.ScreenContextFullName))
                    {
                        testRoot.SubContextTypes = new List<SubContextData>
                        {
                            new SubContextData
                            {
                                // This runs after the domain reload the generated code triggered -
                                // the Root type above was just found in a compiled assembly - so
                                // the screen context resolves from its name here, which it could
                                // not at the moment the file was written.
                                ContextScript = new ContextScriptResolver().ForName(handoff.ScreenContextFullName),
                                ContextFullName = handoff.ScreenContextFullName,
                                ContextName = handoff.ScreenContextFullName.Substring(handoff.ScreenContextFullName.LastIndexOf('.') + 1),
                                AutoSetup = true,
                                IsTest = false
                            }
                        };
                    }
                }
                else
                {
                    Debug.LogWarning($"Root script '{handoff.RootName}' not found in namespace '{handoff.ContextNamespace}'.");
                }

                ScreenManager screenManagerPrefab = AssetDatabase.LoadAssetAtPath<ScreenManager>(CodeGeneratorStrings.SCREEN_MANAGER_PREFAB_PATH);
                ScreenManager screenManager = (ScreenManager) PrefabUtility.InstantiatePrefab(screenManagerPrefab, rootGameObject.transform);

                screenGameObject = new GameObject(prefabName, typeof(RectTransform));
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

                if (!Directory.Exists(handoff.ScreenPrefabPath))
                    Directory.CreateDirectory(handoff.ScreenPrefabPath);

                string finalPrefabPath = handoff.ScreenPrefabPath + "/" + prefabName + ".prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(screenGameObject, finalPrefabPath, InteractionMode.UserAction);

                MakePrefabAddressable(finalPrefabPath, prefabName);
            });

            AssetDatabase.Refresh();
            Selection.activeGameObject = screenGameObject;

            Debug.Log($"Screen prefab '{prefabName}' has been created and marked as Addressable. Scene saved at: {handoff.ScenePath}");
        }

        /// <summary>
        /// The test Root's GameObject is named for the screen without its suffix -
        /// SettingsTestRoot for SettingsScreen - the way it always was.
        /// </summary>
        private static string TrimScreen(string moduleName)
        {
            const string suffix = "Screen";

            return moduleName.EndsWith(suffix, StringComparison.Ordinal)
                ? moduleName.Substring(0, moduleName.Length - suffix.Length)
                : moduleName;
        }
    }
}
#endif