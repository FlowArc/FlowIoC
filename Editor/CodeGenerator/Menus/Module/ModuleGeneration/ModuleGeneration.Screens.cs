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
using UnityEngine;
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
            string parentSharedAssemblyName,
            ModuleGenerationHandoffEVO handoff,
            bool testScreen
        )
        {
            if (testScreen)
            {
                HandleTestScreenModuleCreation(moduleName, modulePath, parentModulePath, directoryConfigMap, actionNames,
                    createScreen, screenSettings, handoff);
                return;
            }

            string testModulePath = Path.Combine(modulePath, testModulesFolderName, $"{moduleName}TestModule");

            CreateFoldersRecursively(testModulePath, directoryConfigMap[ModuleType.Test].RootFolders, selectedOptionalFolders);

            string screenAsmdefName = moduleName + "Module";
            string screenAsmdefPath = Path.Combine(modulePath, screenAsmdefName + ".asmdef");

            // Three assemblies are in play beside the screen's own. The parent's Shared, because a
            // screen reads the data its module publishes and stays out of that module's Models and
            // Commands. The screen's own Shared, when it publishes any data of its own. And the
            // screen's Signals, which is how a Connector reaches it - the same way it reaches any
            // other module, and the only way in, because a screen module generates no Context of
            // its own that anything else could call.
            string screenSharedAssemblyName = new SharedAssemblyDefinition()
                .CreateFor(modulePath, directoryConfigMap[ModuleType.Screen], GetParsedAssemblyName(screenAsmdefName));

            string screenSignalsAssemblyName = new SignalsAssemblyDefinition()
                .CreateFor(modulePath, directoryConfigMap[ModuleType.Screen], GetParsedAssemblyName(screenAsmdefName),
                    screenSharedAssemblyName);

            CreateAssemblyDefinitionFile(screenAsmdefPath, screenAsmdefName, screenSharedAssemblyName,
                screenSignalsAssemblyName, parentSharedAssemblyName);
            AddNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath);
            AddSubAssemblyNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath, screenSharedAssemblyName);
            AddSubAssemblyNamespaceExceptions(directoryConfigMap[ModuleType.Screen], modulePath, screenSignalsAssemblyName);

            string testAsmdefName = moduleName + "TestModule";
            string testAsmdefPath = Path.Combine(testModulePath, testAsmdefName + ".asmdef");
            // The screen's Signals and Shared assemblies are listed as well as the screen's own:
            // asmdef references are not transitive, so a test module that only names the screen
            // could not see the signal holder it drives the screen with.
            CreateAssemblyDefinitionFile(testAsmdefPath, testAsmdefName, GetParsedAssemblyName(screenAsmdefName),
                screenSharedAssemblyName, screenSignalsAssemblyName, parentSharedAssemblyName);
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

            // A screen's signals are not optional the way another module's are: a Connector reaches
            // the screen through its holder, and the screen's own context binds it. It goes in
            // Scripts/Signals, so a Connector can reach it without referencing either the screen's
            // own assembly or the data the screen publishes.
            string publicSignalsPath = directoryConfigMap[ModuleType.Screen]
                .FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, modulePath);

            if (!string.IsNullOrEmpty(publicSignalsPath) && !Directory.Exists(publicSignalsPath))
                publicSignalsPath = null;

            if (string.IsNullOrEmpty(publicSignalsPath))
                publicSignalsPath = signalsPath;

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

            handoff.ViewNamespace = CreateScreenViewAndMediator(viewsAndMediatorsPath, modulePath, moduleName, actionNames,
                false, signalsName, signalsNamespace);

            screenSettings ??= new ScreenModuleSettings {AddressableKey = moduleName};

            // Placed before the context is rendered: an empty Resource path is filled in here, and
            // the context has to load the prefab from the path it is saved under.
            ScreenPrefabPlacement placement = new ScreenPrefabPlacement()
                .For(screenSettings, moduleName, modulePath, screenPrefabPath);

            string contextFullName = CreateScreenContext(rootsAndContextsPath, modulePath, moduleName,
                screenSettings, signalsName, signalsNamespace, out string contextScriptPath);

            RegisterScreenContextOnParentRoot(parentModulePath, directoryConfigMap[ModuleType.Main],
                contextFullName, moduleName + "Context", contextScriptPath);

            handoff.ScreenContextFullName = contextFullName;

            if (createScreen)
            {
                handoff.ScenePath = CreateScene(scenePath, moduleName + "TestScene");
                handoff.ScreenPrefabPath = CreateScreenPrefab(placement.Name, placement.Folder);
                handoff.ScreenPrefabName = placement.Name;
                handoff.ScreenIsAddressable = placement.Addressable;
            }

            handoff.RootName = moduleName + "TestRoot";
            handoff.ContextNamespace = CreateScreenRootAndContext(testRootsAndContextsPath, testModulePath, moduleName, true);
            ShowScreenInLaunch(testRootsAndContextsPath, moduleName + "TestContext", moduleName, modulePath);
        }

        /// <summary>
        /// A screen made for a test module. It is test code, so everything it writes is wrapped in
        /// #if UNITY_EDITOR, its prefab goes to Editor/Resources and loads from there, and it gets
        /// no test module of its own - it is seen in the scene of the test module it was made for,
        /// whose test Root lists it after the reload. Its assembly reaches the module under test
        /// the way that test module does: test code may reference anything, and a sample screen
        /// exists to drive the module it shows.
        /// </summary>
        private static void HandleTestScreenModuleCreation(
            string moduleName,
            string modulePath,
            string testModulePath,
            Dictionary<ModuleType, DirectoryStructureConfig> directoryConfigMap,
            List<string> actionNames,
            bool createScreen,
            ScreenModuleSettings screenSettings,
            ModuleGenerationHandoffEVO handoff)
        {
            DirectoryStructureConfig layout = directoryConfigMap[ModuleType.Screen];
            DirectoryStructureConfig mainLayout = directoryConfigMap[ModuleType.Main];

            string screenAsmdefName = moduleName + "Module";
            string screenAsmdefPath = Path.Combine(modulePath, screenAsmdefName + ".asmdef");

            // The module the test module tests: zTestModules sits in it, and the test module in that.
            string testedModulePath = Path.GetDirectoryName(Path.GetDirectoryName(testModulePath));

            string screenSignalsAssemblyName = new SignalsAssemblyDefinition()
                .CreateFor(modulePath, layout, GetParsedAssemblyName(screenAsmdefName));

            CreateAssemblyDefinitionFile(screenAsmdefPath, screenAsmdefName,
                screenSignalsAssemblyName,
                ParentModuleAssemblyName(testedModulePath),
                new SharedAssemblyDefinition().FindIn(testedModulePath, mainLayout),
                new SignalsAssemblyDefinition().FindIn(testedModulePath, mainLayout));
            AddNamespaceExceptions(layout, modulePath);
            AddSubAssemblyNamespaceExceptions(layout, modulePath, screenSignalsAssemblyName);

            string viewsAndMediatorsPath = layout.FindFullFolderPathByID(FolderEVO.FolderType.ViewsAndMediators, modulePath);
            string rootsAndContextsPath = layout.FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            string signalsPath = layout.FindFullFolderPathByID(FolderEVO.FolderType.Signals, modulePath);
            string publicSignalsPath = layout.FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, modulePath);

            if (string.IsNullOrEmpty(publicSignalsPath) || !Directory.Exists(publicSignalsPath))
                publicSignalsPath = signalsPath;

            string signalsName = null;
            string signalsNamespace = null;

            if (!string.IsNullOrEmpty(publicSignalsPath))
            {
                signalsName = CreateSignals(publicSignalsPath, moduleName + "Signals", "TempSignals",
                    CodeGeneratorStrings.TempSignalsPath, true, true, out signalsNamespace);
            }
            else
            {
                Debug.LogWarning(SIGNALS_WARNING);
            }

            if (!string.IsNullOrEmpty(signalsPath))
            {
                CreateSignals(signalsPath, moduleName + "InternalSignals", "TempInternalSignals",
                    CodeGeneratorStrings.TempInternalSignalsPath, true, false, out _);
            }

            handoff.ViewNamespace = CreateScreenViewAndMediator(viewsAndMediatorsPath, modulePath, moduleName, actionNames,
                true, signalsName, signalsNamespace);

            ScreenPrefabPlacement placement = new ScreenPrefabPlacement().For(screenSettings, moduleName, modulePath,
                layout.FindFullFolderPathByID(FolderEVO.FolderType.Prefabs, modulePath),
                layout.FindFullFolderPathByID(FolderEVO.FolderType.Resources, modulePath));

            string contextFullName = CreateScreenContext(rootsAndContextsPath, modulePath, moduleName,
                screenSettings, signalsName, signalsNamespace, out _, editorOnly: true);

            handoff.IsTestScreen = true;
            handoff.HostModulePath = testModulePath;
            handoff.ScreenContextFullName = contextFullName;
            handoff.ScreenIsAddressable = false;

            if (createScreen)
            {
                handoff.ScreenPrefabPath = CreateScreenPrefab(placement.Name, placement.Folder);
                handoff.ScreenPrefabName = placement.Name;
            }
        }

        /// <summary>
        /// The screen's one declaration: its context, deriving from ScreenSubContext with the view
        /// and mediator as type arguments and the Screen block filled from the window. Returns the
        /// context's full name, which is what a Root's SubContextTypes entry stores.
        ///
        /// <paramref name="contextScriptPath"/> comes back with it because the entry stores the
        /// script asset too, and this is the one place that knows where the file was written. The
        /// type has not compiled yet at this point, so nothing could find the script from the name.
        /// </summary>
        private static string CreateScreenContext(
            string rootsAndContextsPath,
            string modulePath,
            string moduleName,
            ScreenModuleSettings screenSettings,
            string signalsName,
            string signalsNamespace,
            out string contextScriptPath,
            bool editorOnly = false)
        {
            contextScriptPath = null;

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
                contextNamespace, contextName, moduleName + "View", moduleName + "Mediator", viewNamespace, screenSettings,
                editorOnly);

            if (!Directory.Exists(rootsAndContextsPath))
                Directory.CreateDirectory(rootsAndContextsPath);

            string contextPath = rootsAndContextsPath + "/" + contextName + ".cs";
            File.WriteAllText(contextPath, content);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(signalsName))
                CodeGeneratorUtils.BindSignalsInContext(contextPath, signalsName, signalsNamespace);

            contextScriptPath = NamespaceUtility.GetUnityAssetPath(contextPath);

            return $"{contextNamespace}.{contextName}";
        }

        /// <summary>
        /// A screen context is a sub-context of the module it lives in, so the parent's Root prefab
        /// gets the entry. Which prefab that is, out of everything under the parent's Prefabs folder
        /// carrying a RootBase, is ParentRootPrefabPick's answer - MainModule/Prefabs holds
        /// PoolServiceRoot beside MainRoot, and taking whichever came first was right only by
        /// accident of the filesystem's order.
        ///
        /// When the folder has no Root, or more than one and none of them the module's own, nothing
        /// is attached and the warning says why. The step is then the inspector's Add Sub Context,
        /// which is a person choosing rather than a generator guessing.
        /// </summary>
        private static void RegisterScreenContextOnParentRoot(
            string parentModulePath,
            DirectoryStructureConfig parentConfig,
            string contextFullName,
            string contextName,
            string contextScriptPath)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return;

            string prefabsPath = parentConfig.FindFullFolderPathByID(FolderEVO.FolderType.Prefabs, parentModulePath);

            List<string> rootPrefabs = string.IsNullOrEmpty(prefabsPath) || !Directory.Exists(prefabsPath)
                ? new List<string>()
                : Directory.GetFiles(prefabsPath, "*.prefab")
                    .Select(NamespaceUtility.GetUnityAssetPath)
                    .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<RootBase>() != null)
                    .ToList();

            string prefabAssetPath = new ParentRootPrefabPick()
                .From(rootPrefabs, Path.GetFileName(parentModulePath), out string refusal);

            if (prefabAssetPath == null)
            {
                Debug.LogWarning(
                    $"<color=cyan>[FlowIoC]</color> Under '{prefabsPath}' {refusal}, so {contextName} is not attached to a Root yet. "
                    + "Select the parent module's Root, press Add Sub Context in its inspector, pick "
                    + $"{contextName} and leave Auto Setup ticked - the screen registers itself in Setup.");
                return;
            }

            // The script rather than only the name: the entry's reference is what makes deleting or
            // renaming this context something the project can see. It is loaded from the path the
            // file was just written to, because the type has not compiled yet.
            Object contextScript = string.IsNullOrEmpty(contextScriptPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<MonoScript>(contextScriptPath);

            new RootPrefabSubContexts().Add(prefabAssetPath, contextFullName, contextName, contextScript);
            Debug.Log($"<color=cyan>[FlowIoC]</color> {contextName} added to the sub-contexts of '{prefabAssetPath}'.");
        }

        /// <summary>
        /// Writes the View and the Mediator and returns their namespace, which is how the scene
        /// half finds the View type again after the reload.
        /// </summary>
        private static string CreateScreenViewAndMediator(
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

            return viewNamespace;
        }

        /// <summary>
        /// Writes the test module's Root and Context and returns their namespace, which is how
        /// the scene half finds the Root type again after the reload.
        /// </summary>
        private static string CreateScreenRootAndContext(string path, string testModulePath, string moduleName, bool isTest)
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

            return rootsAndContextsNamespace;
        }

        private static void ShowScreenInLaunch(string contextPath, string contextName, string screenName, string modulePath)
        {
            CodeGeneratorUtils.ShowScreenInLaunch(
                contextPath + "/" + contextName + ".cs",
                screenName + "View",
                $"{NamespaceUtility.GetModuleNamespace(modulePath)}.ViewsMediators"
            );
        }

        /// <summary>
        /// Makes the module's scene and saves it at once, under <paramref name="scenesFolder"/>,
        /// and returns its asset path. Saved now rather than after the reload so that the scene
        /// half has a path to find it by: what it edits and saves is the scene at this path, never
        /// whichever scene is active when the scripts come back. The scene is made beside the
        /// open ones and closed again; GeneratedScene.Create says why. The camera Unity put in the
        /// scene clears to a solid colour before the save; GeneratedSceneCamera says why.
        /// </summary>
        private static string CreateScene(string scenesFolder, string sceneName)
        {
            if (!Directory.Exists(scenesFolder))
                Directory.CreateDirectory(scenesFolder);

            string scenePath = NamespaceUtility.GetUnityAssetPath(Path.Combine(scenesFolder, sceneName + ".unity"));
            new GeneratedScene(scenePath).Create();

            return scenePath;
        }

        /// <summary>
        /// The screen's prefab, empty for now: the scene half rebuilds it from the compiled View
        /// under the ScreenManager's first layer and saves it over this one, connected. Returns
        /// the folder it went to, which is where that save goes.
        /// </summary>
        private static string CreateScreenPrefab(string moduleName, string screenPrefabPath)
        {
            if (!Directory.Exists(screenPrefabPath))
            {
                Directory.CreateDirectory(screenPrefabPath);
            }

            string finalPrefabPath = Path.Combine(screenPrefabPath, $"{moduleName}.prefab").Replace("\\", "/");
            GameObject screenObj = new GameObject($"{moduleName}ScreenView", typeof(RectTransform));
            PrefabUtility.SaveAsPrefabAsset(screenObj, finalPrefabPath);
            Object.DestroyImmediate(screenObj);

            return screenPrefabPath;
        }
    }
}
#endif