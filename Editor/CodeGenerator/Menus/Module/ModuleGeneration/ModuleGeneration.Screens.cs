#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static void HandleScreenModuleCreation(
            string moduleName,
            string modulePath,
            string testModulesFolderName,
            List<FolderConfig> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            CodeGeneratorSettings codeGenSettings,
            List<string> actionNames,
            bool createScreen,
            ScreenConfigData screenConfigData,
            string parentSharedAssemblyName
        )
        {
            string testModulePath = Path.Combine(modulePath, testModulesFolderName, $"{moduleName}TestModule");

            CreateFoldersRecursively(testModulePath, directoryConfigMap[ModuleType.Test].RootFolders, selectedOptionalFolders);

            string screenAsmdefName = moduleName + "Module";
            string screenAsmdefPath = Path.Combine(modulePath, screenAsmdefName + ".asmdef");

            // The parent's Shared assembly, not the parent itself: a screen reads the data its
            // module publishes and stays out of that module's Models and Commands. The screen
            // layout has no Shared folder of its own, so this is the only Shared reference it
            // ever gets.
            CreateAssemblyDefinitionFile(screenAsmdefPath, screenAsmdefName, parentSharedAssemblyName);
            AddNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath);

            string testAsmdefName = moduleName + "TestModule";
            string testAsmdefPath = Path.Combine(testModulePath, testAsmdefName + ".asmdef");
            CreateAssemblyDefinitionFile(testAsmdefPath, testAsmdefName, GetParsedAssemblyName(screenAsmdefName),
                parentSharedAssemblyName);
            AddNamespaceExceptions(directoryConfigMap[ModuleType.Test], testModulePath);

            AssetDatabase.Refresh();
            new ModuleIndexRegistrar().Register(
                testModulePath,
                directoryConfigMap[ModuleType.Test],
                codeGenSettings.DirectoryStructureConfigMap.Keys
            );

            string viewsAndMediatorsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderConfig.FolderType.ScreenViews, modulePath);
            string testViewsAndMediatorsPath = directoryConfigMap[ModuleType.Test]
                .FindFullFolderPathByID(FolderConfig.FolderType.ScreenViews, testModulePath);
            string scenePath = directoryConfigMap[ModuleType.Test]
                .FindFullFolderPathByID(FolderConfig.FolderType.Scenes, testModulePath);
            string screenPrefabPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderConfig.FolderType.Prefabs, modulePath);
            string screenConfigPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderConfig.FolderType.ScreenConfigs, modulePath);
            string rootsAndContextsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderConfig.FolderType.RootsAndContexts, modulePath);
            string testRootsAndContextsPath = directoryConfigMap[ModuleType.Test]
                .FindFullFolderPathByID(FolderConfig.FolderType.RootsAndContexts, testModulePath);
            string signalsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderConfig.FolderType.Signals, modulePath);

            // A screen's signals are not optional the way another module's are: the screen module
            // generates no Context of its own, so the holder is the only surface anything outside
            // the screen has to talk to it through.
            string signalsName = null;
            string signalsNamespace = null;

            if (!string.IsNullOrEmpty(signalsPath))
            {
                // The holder belongs to the screen module itself, which ships in a build, so it is
                // never the Editor-only kind - the test module beside it has its own.
                signalsName = CreateSignals(signalsPath, modulePath, moduleName, false, out signalsNamespace);
            }
            else
            {
                Debug.LogWarning(SIGNALS_WARNING);
            }

            CreateScreenViewAndMediator(viewsAndMediatorsPath, modulePath, moduleName, actionNames, false, signalsName, signalsNamespace);

            if (createScreen)
            {
                CreateTestScene(scenePath, moduleName);
                GameObject prefabAsset = CreateScreenPrefab(moduleName, screenPrefabPath);
                CreateScreenConfig(screenConfigPath, moduleName, screenConfigData, prefabAsset);
                EditorPrefs.SetBool(BOOL_CREATE_SCREEN, true);
            }

            CreateScreenRootAndContext(testRootsAndContextsPath, testModulePath, moduleName, true);
            BindScreenMediationInTestContext(testRootsAndContextsPath, modulePath, moduleName);

            if (!string.IsNullOrEmpty(signalsName))
            {
                CodeGeneratorUtils.BindSignalsInContext(
                    testRootsAndContextsPath + "/" + moduleName + "TestContext.cs", signalsName, signalsNamespace);
            }

            ShowScreenInLaunch(testRootsAndContextsPath, moduleName + "TestContext", moduleName, "Runtime");
        }

        private static void CreateScreenViewAndMediator(
            string path,
            string modulePath,
            string moduleName,
            List<string> actionNames,
            bool isTest,
            string signalsName = null,
            string signalsNamespace = null
        )
        {
            string suffix = isTest ? "" : "";
            string viewName = moduleName + suffix + "View";
            string mediatorName = moduleName + suffix + "Mediator";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string viewNamespace = $"{moduleNamespace}.ViewsMediators";
            string mediatorNamespace = $"{moduleNamespace}.ViewsMediators";

            CodeGeneratorUtils.CreateView(
                viewName,
                "TempScreenView",
                path,
                CodeGeneratorStrings.TempScreenViewPath,
                viewNamespace,
                actionNames,
                isTest
            );
            CodeGeneratorUtils.CreateMediator(
                mediatorName,
                viewName,
                "TempScreenMediator",
                path,
                CodeGeneratorStrings.TempScreenMediatorPath,
                mediatorNamespace,
                actionNames,
                isTest,
                signalsName,
                signalsNamespace
            );

            EnsureNamespaceImport(mediatorName, path, "ViewsMediators");
            EnsureNamespaceImport(viewName, path, "ViewsMediators");

            EditorPrefs.SetString(KEY_FILE_NAME, viewName);
            EditorPrefs.SetString(KEY_MODULE_NAME, moduleName);
            EditorPrefs.SetString(KEY_PARENT_FOLDER_PATH, moduleName);
            EditorPrefs.SetString(KEY_VIEW_NAMESPACE, viewNamespace);
        }

        private static void CreateScreenRootAndContext(string path, string testModulePath, string moduleName, bool isTest)
        {
            string suffix = isTest ? "Test" : "";
            string rootName = moduleName + suffix + "Root";
            string contextName = moduleName + suffix + "Context";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(testModulePath);
            string rootsAndContextsNamespace = $"{moduleNamespace}.RootsContexts";

            CodeGeneratorUtils.CreateContext(
                contextName,
                "TempScreenTestContext",
                path,
                CodeGeneratorStrings.TempScreenTestContextPath,
                rootsAndContextsNamespace,
                true,
                isTest
            );
            CodeGeneratorUtils.CreateRoot(
                rootName,
                contextName,
                "TempScreenTestContext",
                "TempScreenTestRoot",
                path,
                CodeGeneratorStrings.TempScreenTestRootPath,
                rootsAndContextsNamespace,
                isTest
            );

            EditorPrefs.SetString(KEY_CONTEXT_NAMESPACE, rootsAndContextsNamespace);
        }

        private static void BindScreenMediationInContext(string contextPath, string modulePath, string moduleName)
        {
            string viewName = moduleName + "View";
            string mediatorName = moduleName + "Mediator";
            string contextName = moduleName + "Context";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string viewNamespace = $"{moduleNamespace}.ViewsMediators";

            CodeGeneratorUtils.BindMediationInContext(
                contextPath + "/" + contextName + ".cs",
                viewName,
                mediatorName,
                viewNamespace
            );
        }

        private static void BindScreenMediationInTestContext(string contextPath, string modulePath, string moduleName)
        {
            string contextName = moduleName + "TestContext";

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string viewNamespace = $"{moduleNamespace}.ViewsMediators";

            CodeGeneratorUtils.BindMediationInScreenContext(
                contextPath + "/" + contextName + ".cs",
                viewNamespace
            );
        }

        private static void ShowScreenInLaunch(string contextPath, string contextName, string screenName, string parentFolderPath)
        {
            CodeGeneratorUtils.ShowScreenInLaunch(
                contextPath + "/" + contextName + ".cs",
                screenName + "View",
                screenName,
                parentFolderPath
            );
        }

        private static void CreateScene(string scenePath, string moduleName)
        {
            if (!Directory.Exists(scenePath))
                Directory.CreateDirectory(scenePath);

            string sceneName = moduleName + "Scene";
            EditorPrefs.SetString(KEY_MODULE_NAME, moduleName);
            EditorPrefs.SetString(KEY_SCREEN_NAME, sceneName);
            EditorPrefs.SetString(KEY_SCENE_PATH, scenePath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = sceneName;
        }

        private static void CreateTestScene(string scenePath, string moduleName)
        {
            if (!Directory.Exists(scenePath))
                Directory.CreateDirectory(scenePath);

            string sceneName = moduleName + "TestScene";
            EditorPrefs.SetString(KEY_SCREEN_NAME, sceneName);
            EditorPrefs.SetString(KEY_SCENE_PATH, scenePath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = sceneName;
        }

        private static GameObject CreateScreenPrefab(string moduleName, string screenPrefabPath)
        {
            EditorPrefs.SetString(SCREEN_PREFAB_PATH, screenPrefabPath);

            if (!Directory.Exists(screenPrefabPath))
            {
                Directory.CreateDirectory(screenPrefabPath);
            }

            string finalPrefabPath = Path.Combine(screenPrefabPath, $"{moduleName}.prefab").Replace("\\", "/");
            GameObject screenObj = new GameObject($"{moduleName}ScreenView", typeof(RectTransform));
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(screenObj, finalPrefabPath);
            Object.DestroyImmediate(screenObj);
            EditorPrefs.SetString(PREF_KEY_SCREEN_PREFAB_PATH, finalPrefabPath);

            return prefabAsset;
        }
    }
}
#endif