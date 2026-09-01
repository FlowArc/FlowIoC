#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The input module: what it announces, the action asset it brings, and the button that puts
    /// it in the project.
    ///
    /// It is not part of the setup set on purpose. A scene needs an EventSystem for its buttons to
    /// answer, and MainScene carries one of its own - which is an ordinary Unity component, not a
    /// reason to install a module. This module is for a game that wants its input as signals.
    /// </summary>
    internal class InputModulePage : HelpPage
    {
        private const string ModuleFolderName = "InputModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        /// <summary>
        /// The module's assembly references Unity.InputSystem, so copying it into a project that
        /// does not have the package would stop the whole project compiling.
        /// </summary>
        private readonly string[] _requiredPackages = {"com.unity.inputsystem"};

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public InputModulePage() : base(null)
        {
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Input";

        public override string Subtitle => "Pointer as signals";

        public override string Icon => "d_UnityEditor.GameView";

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Usage", DrawUsage)
        };

        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        /// <summary>
        /// Packages first. The module's asmdef names Unity.InputSystem, so a missing package is
        /// asked about rather than discovered as a project that will not compile.
        /// </summary>
        private void Install()
        {
            _checkedAt = double.NegativeInfinity;

            IReadOnlyList<string> missing =
                new MissingPackages().In(new InstalledPackages().Ids(), _requiredPackages);

            if (missing.Count > 0)
            {
                bool add = EditorUtility.DisplayDialog(
                    "Input",
                    $"The module references {string.Join(" and ", missing)}, which this project "
                    + "does not have.\n\n"
                    + "Adding them writes to Packages/manifest.json and reimports the project. The "
                    + "module installs itself once that has finished.",
                    "Add and install",
                    "Cancel");

                if (add)
                    new PendingModuleInstall().Begin(ModuleFolderName, missing);

                return;
            }

            if (_installer.TryInstall(ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog(
                    "Input installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "Drop InputRoot into the scene that needs it - see the Usage tab.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("Input", error, "OK");
        }

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Turns the pointer into signals. A press, the drags that follow it and the release "
                + "are announced to whoever is listening, so nothing in the game has to read the "
                + "Input System itself.");

            painter.SubHeading("What it gives you");
            painter.Bullet(
                "Three outgoing signals: PointerPressed, PointerDragged and PointerReleased, each "
                + "carrying the screen position. Dragged is announced only while the pointer is "
                + "down - a signal per mouse move would be a dispatch per frame for something "
                + "almost no game wants.");
            painter.Bullet(
                "One incoming signal: SetActionMapEnabled, which turns an action map on or off by "
                + "name. A game silences gameplay input while a screen is open by disabling the "
                + "map, rather than by ignoring what arrives.");
            painter.Bullet(
                "An action asset of its own, FlowIoCInputActions, with a Pointer map bound to the "
                + "mouse and to touch. It is yours once it lands: add maps, rebind, rename.");

            painter.Space();
            painter.Note(
                "The EventSystem is not its job. A scene that shows uGUI needs one whether this "
                + "module is installed or not, so MainScene carries an ordinary EventSystem and "
                + "the module stays about actions.");

            painter.SubHeading("What lands in the project");
            painter.Paragraph(
                "Assets/Modules/InputModule: the Modules.Input assembly and its Shared assembly, "
                + "an InputRoot prefab carrying the input view, and the action asset. Nothing "
                + "outside that folder is touched.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.Paragraph(
                "Drop InputRoot into the scene. The prefab holds the root, the input view and "
                + "the ViewInjector entry that binds them, so there is nothing to wire by hand.");
            painter.Paragraph(
                "Then listen where you need it. The signal holder lives in the module's Shared "
                + "assembly, so a Connector reaches it through Modules.Input.Shared:");
            painter.Code(
                "private InputSignals _inputSignals;\n"
                + "private HeroSignals  _heroSignals;\n"
                + "\n"
                + "public override void Setup()\n"
                + "{\n"
                + "    _inputSignals = InjectionBinderCrossContext.GetInstance<InputSignals>();\n"
                + "    _heroSignals  = InjectionBinderCrossContext.GetInstance<HeroSignals>();\n"
                + "\n"
                + "    _inputSignals.Outgoing.PointerDragged\n"
                + "        .Connect(_heroSignals.Incoming.AimAt);\n"
                + "}");

            painter.Space();
            painter.SubHeading("Silencing it");
            painter.Paragraph(
                "Disabling a map stops the dispatches at the source. The map name is whatever the "
                + "action asset calls it - Pointer, until the game adds its own:");
            painter.Code("_inputSignals.Incoming.SetActionMapEnabled.Dispatch(\"Pointer\", false);");

            painter.Space();
            painter.Note(
                "The view reads the asset assigned on the prefab, not the project-wide input "
                + "actions. Point it at an asset of your own and the module follows it.");
        }
    }
}

#endif