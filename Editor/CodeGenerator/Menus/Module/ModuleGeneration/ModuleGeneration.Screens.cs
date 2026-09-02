#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Root;
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
            string parentModulePath,
            string testModulesFolderName,
            List<FolderEVO> selectedOptionalFolders,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            ED_CodeGenerator codeGenSettings,
            List<string> actionNames,
            bool createScreen,
            ScreenModuleSettings screenSettings,
            string parentSharedAssemblyName
        )
        {
            string testModulePath = Path.Combine(modulePath, testModulesFolderName, $"{moduleName}TestModule");

            CreateFoldersRecursively(testModulePath, directoryConfigMap[ModuleType.Test].RootFolders, selectedOptionalFolders);

            string screenAsmdefName = moduleName + "Module";
            string screenAsmdefPath = Path.Combine(modulePath, screenAsmdefName + ".asmdef");

            // Two Shared assemblies are in play. The parent's, because a screen reads the data its
            // module publishes and stays out of that module's Models and Commands. And the
            // screen's own, because that is where its signal holder lives - a Connector reaches a
            // screen the same way it reaches any other module, through Modules.X.Shared.
            string screenSharedAssemblyName = new SharedAssemblyDefinition()
                .CreateFor(modulePath, directoryConfigMap[ModuleType.Screen], GetParsedAssemblyName(screenAsmdefName));

            CreateAssemblyDefinitionFile(screenAsmdefPath, screenAsmdefName, screenSharedAssemblyName, parentSharedAssemblyName);
            AddNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath);
            AddSharedNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath, screenSharedAssemblyName);

            string testAsmdefName = moduleName + "TestModule";
            string testAsmdefPath = Path.Combine(testModulePath, testAsmdefName + ".asmdef");
            // The screen's own Shared assembly is listed as well as the screen's: asmdef references
            // are not transitive, so a test module that only names the screen could not see the
            // signal holder the screen publishes.
            CreateAssemblyDefinitionFile(testAsmdefPath, testAsmdefName, GetParsedAssemblyName(screenAsmdefName),
                screenSharedAssemblyName, parentSharedAssemblyName);
            AddNamespaceExceptions(directoryConfigMap[ModuleType.Test], testModulePath);

            AssetDatabase.Refresh();
            new ModuleIndexRegistrar().Register(
                testModulePath,
                directoryConfigMap[ModuleType.Test],
                codeGenSettings.DirectoryStructureConfigMap.Keys
            );

            string viewsAndMediatorsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.ViewsAndMediators, modulePath);
            string scenePath = directoryConfigMap[ModuleType.Test]
                .FindFullFolderPathByID(FolderEVO.FolderType.Scenes, testModulePath);
            string screenPrefabPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.Prefabs, modulePath);
            string rootsAndContextsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string testRootsAndContextsPath = directoryConfigMap[ModuleType.Test]
                .FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, testModulePath);
            string signalsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.Signals, modulePath);

            string sharedSignalsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.SharedSignals, modulePath);

            if (!string.IsNullOrEmpty(sharedSignalsPath) && !Directory.Exists(sharedSignalsPath))
                sharedSignalsPath = null;

            // A screen's signals are not optional the way another module's are: a Connector reaches
            // the screen through its holder, and the screen's own context binds it. It goes in
            // Shared, so a Connector can reach it without referencing the screen's own assembly.
            string publicSignalsPath = string.IsNullOrEmpty(sharedSignalsPath) ? signalsPath : sharedSignalsPath;

            string signalsName = null;
            string signalsNamespace = null;

            if (!string.IsNullOrEmpty(publicSignalsPath))
            {
                // The holder belongs to the screen module itself, which ships in a build, so it is
                // never the Editor-only kind - the test module beside it has its own.
                signalsName = CreateSignals(publicSignalsPath, moduleName + "Signals", "TempSignals",
                    CodeGeneratorStrings.TempSignalsPath, false, true, out signalsNamespace);
            }
            else
            {
                Debug.LogWarning(SIGNALS_WARNING);
            }

            if (!string.IsNullOrEmpty(signalsPath))
            {
                CreateSignals(signalsPath, moduleName + "InternalSignals", "TempInternalSignals",
                    CodeGeneratorStrings.TempInternalSignalsPath, false, false, out _);
            }

            CreateScreenViewAndMediator(viewsAndMediatorsPath, modulePath, moduleName, actionNames, false, signalsName, signalsNamespace);

            string contextFullName = CreateScreenContext(rootsAndContextsPath, modulePath, moduleName,
                screenSettings ?? new ScreenModuleSettings {AddressableKey = moduleName}, signalsName, signalsNamespace);

            RegisterScreenContextOnParentRoot(parentModulePath, directoryConfigMap[ModuleType.Main],
                contextFullName, moduleName + "Context");

            EditorPrefs.SetString(KEY_SCREEN_CONTEXT_FULL_NAME, contextFullName);

            if (createScreen)
            {
                CreateTestScene(scenePath, moduleName);
                CreateScreenPrefab(moduleName, screenPrefabPath);
                EditorPrefs.SetBool(BOOL_CREATE_SCREEN, true);
            }

            CreateScreenRootAndContext(testRootsAndContextsPath, testModulePath, moduleName, true);
            ShowScreenInLaunch(testRootsAndContextsPath, moduleName + "TestContext", moduleName, modulePath);
        }

        /// <summary>
        /// The screen's one declaration: its context, deriving from ScreenSubContext with the view
        /// and mediator as type arguments and the Screen block filled from the window. Returns the
        /// context's full name, which is what a Root's SubContextTypes entry stores.
        /// </summary>
        private static string CreateScreenContext(
            string rootsAndContextsPath,
            string modulePath,
            string moduleName,
            ScreenModuleSettings screenSettings,
            string signalsName,
            string signalsNamespace)
        {
            if (string.IsNullOrEmpty(rootsAndContextsPath))
            {
                Debug.LogWarning(ROOTS_CONTEXTS_WARNING);
                return null;
            }

            string moduleNamespace = NamespaceUtility.GetModuleNamespace(modulePath);
            string contextNamespace = $"{moduleNamespace}.RootsContexts";
            string viewNamespace = $"{moduleNamespace}.ViewsMediators";
            string contextName = moduleName + "Context";

            string content = new ScreenContextTemplate().Render(
                contextNamespace, contextName, moduleName + "View", moduleName + "Mediator", viewNamespace, screenSettings);

            if (!Directory.Exists(rootsAndContextsPath))
                Directory.CreateDirectory(rootsAndContextsPath);

            string contextPath = rootsAndContextsPath + "/" + contextName + ".cs";
            File.WriteAllText(contextPath, content);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(signalsName))
                CodeGeneratorUtils.BindSignalsInContext(contextPath, signalsName, signalsNamespace);

            return $"{contextNamespace}.{contextName}";
        }

        /// <summary>
        /// A screen context is a sub-context of the module it lives in, so the parent's Root prefab
        /// gets the entry. The prefab is whichever one under the parent's Prefabs folder carries a
        /// RootBase. When there is none - a parent created without a Root, or one kept in a scene -
        /// the step is left to the inspector's Add Sub Context, and says so.
        /// </summary>
        private static void RegisterScreenContextOnParentRoot(
            string parentModulePath,
            DirectoryStructureConfig parentConfig,
            string contextFullName,
            string contextName)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return;

            string prefabsPath = parentConfig.FindFullFolderPathByID(FolderEVO.FolderType.Prefabs, parentModulePath);

            string prefabAssetPath = string.IsNullOrEmpty(prefabsPath) || !Directory.Exists(prefabsPath)
                ? null
                : Directory.GetFiles(prefabsPath, "*.prefab")
                    .Select(NamespaceUtility.GetUnityAssetPath)
                    .FirstOrDefault(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<RootBase>() != null);

            if (prefabAssetPath == null)
            {
                Debug.LogWarning(
                    $"<color=cyan>[FlowIoC]</color> No Root prefab was found under '{prefabsPath}', so {contextName} is not attached to a Root yet. "
                    + "Select the parent module's Root, press Add Sub Context in its inspector, pick "
                    + $"{contextName} and leave Auto Setup ticked - the screen registers itself in Setup.");
                return;
            }

            new RootPrefabSubContexts().Add(prefabAssetPath, contextFullName, contextName);
            Debug.Log($"<color=cyan>[FlowIoC]</color> {contextName} added to the sub-contexts of '{prefabAssetPath}'.");
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

        private static void ShowScreenInLaunch(string contextPath, string contextName, string screenName, string modulePath)
        {
            CodeGeneratorUtils.ShowScreenInLaunch(
                contextPath + "/" + contextName + ".cs",
                screenName + "View",
                $"{NamespaceUtility.GetModuleNamespace(modulePath)}.ViewsMediators"
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

        private static void CreateScreenPrefab(string moduleName, string screenPrefabPath)
        {
            EditorPrefs.SetString(SCREEN_PREFAB_PATH, screenPrefabPath);

            if (!Directory.Exists(screenPrefabPath))
            {
                Directory.CreateDirectory(screenPrefabPath);
            }

            string finalPrefabPath = Path.Combine(screenPrefabPath, $"{moduleName}.prefab").Replace("\\", "/");
            GameObject screenObj = new GameObject($"{moduleName}ScreenView", typeof(RectTransform));
            PrefabUtility.SaveAsPrefabAsset(screenObj, finalPrefabPath);
            Object.DestroyImmediate(screenObj);
        }
    }
}
#endif