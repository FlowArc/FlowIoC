#if UNITY_EDITOR

using System.IO;
using System.Linq;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.CodeGenerator.Detector;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="SetupModulesStartup"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class SetupModulesStartupHook
    {
        static SetupModulesStartupHook()
        {
            EditorApplication.delayCall += () => new SetupModulesStartup().Run();
        }
    }

    /// <summary>
    /// Installs the modules a FlowIoC game starts with, the first time the Editor opens on a
    /// project that has none of its own.
    ///
    /// The set is not reference material: it is where the game's code will live. So unlike the
    /// agent skills, which come back when deleted, this runs once and records that it has. A game
    /// that deletes one of the modules has decided something, and the next session respects it.
    /// </summary>
    internal class SetupModulesStartup
    {
        private const string MainScenePath = "Assets/Modules/MainModule/Scenes/MainScene.unity";

        private readonly string _projectRoot;
        private readonly SetupState _state;
        private readonly SetupModulesInstaller _installer;

        internal SetupModulesStartup()
        {
            _projectRoot = new ProjectRoot().Resolve();
            _state = new SetupState(_projectRoot);
            _installer = new SetupModulesInstaller(
                _projectRoot, new ModulesSource(PackageRoot(), ModulesSource.SetupModulesFolder));
        }

        internal void Run()
        {
            SetupInstallDecision decision = new SetupInstallRule().For(
                _state.IsInstalled(), Application.isBatchMode, AnyModulePresent());

            switch (decision)
            {
                case SetupInstallDecision.Stop:
                    return;

                case SetupInstallDecision.MarkOnly:
                    _state.MarkInstalled(PackageVersion());
                    return;

                case SetupInstallDecision.Install:
                    InstallNow();
                    return;
            }
        }

        /// <summary>
        /// The install itself, without the rule in front of it. The Help window's button calls
        /// this: pressing it is somebody asking for the set, and the marker only records whether
        /// the automatic install has had its turn.
        /// </summary>
        internal void InstallNow()
        {
            SetupInstallReport report = _installer.Install();

            if (!report.Succeeded)
            {
                Debug.LogWarning($"<color=cyan>[FlowIoC]</color> The setup modules were not installed: {report.Blocked}");
                _state.MarkInstalled(PackageVersion());
                return;
            }

            foreach (string name in report.Installed)
                Debug.Log($"<color=cyan>[FlowIoC]</color> Setup module installed: {SetupModulesInstaller.TargetFolder}/{name}");

            RegisterWithUnity();
            RegisterAddressables(report.Installed);
            OpenTheScene();

            _state.MarkInstalled(PackageVersion());
        }

        /// <summary>
        /// What turns copied folders into modules the rest of the Editor knows about, done once for
        /// the whole set. The order matters: the index has to know the modules before the namespace
        /// settings can be written from them.
        /// </summary>
        private void RegisterWithUnity()
        {
            // The settings asset is what the module index is rebuilt through, and on a project this
            // hook has never run on there is none: the generator menus create it, and nobody has
            // opened one yet. Without it the index stays empty, FlowLogType is written with no
            // channels, and the modules that just landed - every one of them logging on its own
            // channel - take the project down with them.
            ED_CodeGenerator.CreateConfig();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ModuleAutoDetector.RescanModules();
            FlowLogTypeGenerator.Generate();
            new ModuleRepair().FixAll();

            // Here, and not from inside RescanModules: the repair above writes the settings files
            // that a scan taken a line earlier would have reported as missing.
            new ModuleScannerStartupReport().Report();
        }

        /// <summary>
        /// Every screen the set brought, made addressable the way Create Module makes one it wrote.
        /// The prefabs are found by convention rather than from a list in the payload: a list would
        /// be one more file to keep in step with the modules beside it.
        ///
        /// Only the folders this run wrote are walked. The automatic install happens in a project
        /// with no modules at all, but the Help window's button does not, and a game's own screens
        /// are none of this method's business.
        /// </summary>
        private void RegisterAddressables(string[] installedModules)
        {
            var entries = new ScreenAddressableEntries();
            var addressables = new ScreenAddressables();
            var registered = false;

            foreach (string moduleFolderName in installedModules)
            {
                string module = _installer.TargetOf(moduleFolderName);

                if (!Directory.Exists(module))
                    continue;

                foreach (string prefab in Directory.GetFiles(module, "*Screen.prefab", SearchOption.AllDirectories))
                {
                    ScreenAddressableEntry entry = entries.For(Path.GetFileNameWithoutExtension(prefab));
                    entry.AssetPath = AssetPath(prefab);

                    addressables.Register(entry);
                    registered = true;
                }
            }

            // ScreenAddressables leaves saving to its caller, so the whole set is written back out
            // once rather than after each of the entries above.
            if (registered)
                AssetDatabase.SaveAssets();
        }

        private string AssetPath(string fullPath)
        {
            string assets = Path.Combine(_projectRoot, "Assets");

            return "Assets" + fullPath.Substring(assets.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// The scene goes to the front of the build list and is opened, so pressing Play shows the
        /// flow the set brought. Whatever is open is offered for saving first.
        /// </summary>
        private void OpenTheScene()
        {
            if (!File.Exists(Path.Combine(_projectRoot, MainScenePath)))
                return;

            string[] existing = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            string[] wanted = new BuildSceneList().WithSceneFirst(existing, MainScenePath);

            if (wanted.Length != existing.Length)
            {
                EditorBuildSettings.scenes = wanted
                    .Select(path => new EditorBuildSettingsScene(path, true))
                    .ToArray();
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// True when the project already has a module of its own. Asked of Assets/Modules
        /// specifically - the only place FlowIoC puts a module - because a third party plugin with
        /// an asmdef elsewhere under Assets is not a module and should not suppress the install.
        /// </summary>
        private bool AnyModulePresent()
        {
            string modules = Path.Combine(_projectRoot, "Assets", "Modules");

            if (!Directory.Exists(modules))
                return false;

            foreach (string folder in Directory.GetDirectories(modules))
            {
                if (Directory.GetFiles(folder, "*.asmdef", SearchOption.TopDirectoryOnly).Length > 0)
                    return true;
            }

            return false;
        }

        internal bool IsInstalled() => _installer.IsInstalled();

        private static string PackageRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SetupModulesStartup).Assembly);

            return info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");
        }

        private static string PackageVersion()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SetupModulesStartup).Assembly);

            return info == null ? "unknown" : info.version;
        }
    }
}

#endif