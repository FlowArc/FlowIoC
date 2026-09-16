#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>
    /// The set-level install as a button runs it: the Input System offered first, then the whole
    /// set. Shared by the Overview page and by a setup module's page while none of the set is
    /// here, so "Install All Setup" means the same thing wherever it is pressed.
    ///
    /// The package is offered before the copy rather than demanded instead of it. Nothing in the
    /// set references the Input System from C# - the dependency is the input module component on
    /// the EventSystem authored in MainScene - so a project without the package still compiles,
    /// and the only thing missing is the script on that component.
    /// </summary>
    internal class SetupSetInstallAction
    {
        private const string InputSystemPackage = "com.unity.inputsystem";

        private readonly SetupModulesStartup _setup;

        internal SetupSetInstallAction() : this(new SetupModulesStartup())
        {
        }

        internal SetupSetInstallAction(SetupModulesStartup setup)
        {
            _setup = setup;
        }

        internal void Run()
        {
            OfferInputSystem();
            _setup.InstallNow();
        }

        private static void OfferInputSystem()
        {
            IReadOnlyList<string> missing = new MissingPackages()
                .In(new InstalledPackages().Ids(), new[] {InputSystemPackage});

            if (missing.Count == 0)
                return;

            bool add = EditorUtility.DisplayDialog(
                "Setup Modules",
                $"MainScene carries an EventSystem that reads through {InputSystemPackage}, which "
                + "this project does not have. Without it the buttons in MainScene answer nothing."
                + "\n\nAdding it writes to Packages/manifest.json and reimports the project.",
                "Add it",
                "Not now");

            if (add)
                Client.Add(InputSystemPackage);
        }
    }
}

#endif
