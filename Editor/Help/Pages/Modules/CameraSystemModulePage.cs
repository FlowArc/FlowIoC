#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The camera system module: the cameras it names, how a game hands its own Cinemachine
    /// cameras over, what it does not wire up for you, and the button that puts it in the project.
    ///
    /// Unlike the counter module this module has packages behind it, so installing it may have
    /// to add them first. PendingModuleInstall carries the install across that.
    /// </summary>
    internal class CameraSystemModulePage : HelpPage
    {
        private const string ModuleFolderName = "CameraSystemModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        /// <summary>
        /// What the module's two assemblies reference. Cinemachine is the module's subject;
        /// the render pipeline core is where SerializedDictionary comes from, which is how a
        /// camera adapter maps a name to its configuration in the Inspector.
        /// </summary>
        private readonly string[] _requiredPackages =
        {
            "com.unity.cinemachine",
            "com.unity.render-pipelines.core"
        };

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public CameraSystemModulePage() : base(null)
        {
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Camera System";

        public override string Subtitle => "Named Cinemachine cameras";

        public override string Icon => "Camera Icon";

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Usage", DrawUsage),
            new HelpTab("Wiring", DrawWiring)
        };

        /// <summary>
        /// Whether the module is in the project, from a cache that goes stale after a second. The
        /// check underneath walks every asmdef under Assets and the banner asks twice a repaint.
        /// </summary>
        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        /// <summary>
        /// Packages first. Copying a module whose asmdef references an assembly the project does
        /// not have stops the whole project compiling, so a missing package is asked about rather
        /// than discovered afterwards.
        /// </summary>
        private void Install()
        {
            _checkedAt = double.NegativeInfinity;

            IReadOnlyList<string> missing =
                new MissingPackages().In(new InstalledPackages().Ids(), _requiredPackages);

            if (missing.Count > 0)
            {
                bool add = EditorUtility.DisplayDialog(
                    "Camera System",
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
                    "Camera System installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "Nothing else in the project was touched - see the Wiring tab for the one "
                    + "thing left to do.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("Camera System", error, "OK");
        }

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Gives the game's Cinemachine cameras names and switches between them by signal. "
                + "A menu camera and a gameplay camera come with it; a game that needs more adds "
                + "them to one enum.");

            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A camera is a CameraName, not a scene reference. Switching is one dispatch, and "
                + "nothing that switches has to know which GameObject the camera sits on.");
            painter.Bullet(
                "Cameras register themselves. An adapter on the rig hands its cameras over when "
                + "the scene loads and takes them back when it unloads, so a scene change does not "
                + "leave the model holding cameras that are gone.");
            painter.Bullet(
                "It remembers where a camera was. SetCameraLastPos stores a position per camera "
                + "and MoveCameraToLastPos returns to it, which is what a game needs when the "
                + "player comes back from a menu.");
            painter.Bullet(
                "Custom blends are data. CD_CameraCustomBlends holds the Cinemachine blend table, "
                + "and a camera's own entry can override it as it registers.");

            painter.Space();
            painter.Note(
                "It is a System, not a Service: it is specific to the game it sits in. No other "
                + "module references it. What reaches it are signals, wired in a Connector - which "
                + "the install deliberately does not write for you. The Wiring tab has it.");

            painter.SubHeading("What lands in the project");
            painter.Bullet("Modules.CameraSystem - the model, the commands, the adapters.");
            painter.Bullet(
                "Modules.CameraSystem.Shared - the CameraName enum on its own, so a module that "
                + "names a camera references the data and not the module.");
            painter.Bullet("Prefabs/CameraRoot - the module's presence in the scene.");
            painter.Bullet("Scriptables/CD_CameraCustomBlends - the blend table.");

            painter.SubHeading("What it needs");
            painter.Paragraph(
                "com.unity.cinemachine, and com.unity.render-pipelines.core for the "
                + "SerializedDictionary the multi-camera adapter is authored through. The Install "
                + "button checks both and offers to add whichever is absent before it copies "
                + "anything.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.Paragraph(
                "Put CameraRoot in the scene, then tell the module which cameras it has. A rig "
                + "with several cameras carries one CameraAdapterView; a single camera carries "
                + "SingleCameraAdapterView instead. Both register on their own - there is no call "
                + "to make.");

            painter.SubHeading("Naming a camera");
            painter.Paragraph(
                "CameraName is the module's vocabulary and lives in its Shared assembly. Add the "
                + "entries the game needs and fill the adapter's map in the Inspector.");
            painter.Code(
                "public enum CameraName\n"
                + "{\n"
                + "    Menu,\n"
                + "    Gameplay,\n"
                + "    Cutscene\n"
                + "}");

            painter.SubHeading("Switching");
            painter.Paragraph(
                "Everything the module does is an incoming signal, so a Command drives it the way "
                + "it drives anything else.");
            painter.Code(
                "[InjectSignal] private CameraSignals _cameraSignals { get; set; }\n"
                + "\n"
                + "_cameraSignals.Incoming.SwitchCamera.Dispatch(CameraName.Gameplay);\n"
                + "_cameraSignals.Incoming.SetCameraTarget.Dispatch(_playerTransform);");

            painter.SubHeading("Coming back to where you were");
            painter.Paragraph(
                "Store a camera's position before leaving it and move back to it afterwards. The "
                + "float is how long the move takes.");
            painter.Code(
                "_cameraSignals.Incoming.SetCameraLastPos.Dispatch(CameraName.Gameplay);\n"
                + "_cameraSignals.Incoming.MoveCameraToLastPos.Dispatch(CameraName.Gameplay, 0.4f);");

            painter.SubHeading("Asking who the target is");
            painter.Paragraph(
                "PublishCameraTarget asks; the answer comes back on Outgoing.CameraTargetReady, "
                + "which is what a Connector listens to. A module that wants the target does not "
                + "read it - it is told.");
            painter.Code(
                "_cameraSignals.Incoming.PublishCameraTarget.Dispatch();");

            painter.Space();
            painter.Note(
                "The other incoming signals are MoveCamera, SetCameraDistance, RegisterCamera and "
                + "UnregisterCamera. The last two are what the adapters dispatch, so a game rarely "
                + "sends them itself.");
        }

        private void DrawWiring(HelpPainter painter)
        {
            painter.Paragraph(
                "Installing copies the module's folder and nothing else. It does not add a "
                + "reference to your Connector's assembly, does not write a sub-context into it, "
                + "and does not touch ConnectorRoot. A tool that edited another module's files "
                + "behind your back would be harder to trust than the two minutes this takes.");

            painter.SubHeading("One reference");
            painter.Paragraph(
                "Add Modules.CameraSystem to the references of the assembly that holds your "
                + "connectors. That assembly is the only one in the project allowed to see both "
                + "sides of a wire.");

            painter.SubHeading("One sub-context");
            painter.Paragraph(
                "Name it after the module on the other side of the wire and split the two "
                + "directions, so a reader can see at a glance what leaves and what arrives.");
            painter.Code(
                "public class CameraConnectorSubContext : Context\n"
                + "{\n"
                + "    private MainSignals   _mainSignals;\n"
                + "    private CameraSignals _cameraSignals;\n"
                + "\n"
                + "    public override void Setup()\n"
                + "    {\n"
                + "        base.Setup();\n"
                + "\n"
                + "        _mainSignals   = InjectionBinderCrossContext.GetInstance<MainSignals>();\n"
                + "        _cameraSignals = InjectionBinderCrossContext.GetInstance<CameraSignals>();\n"
                + "\n"
                + "        IncomingSignals();\n"
                + "        OutGoingSignals();\n"
                + "    }\n"
                + "\n"
                + "    private void IncomingSignals() =>\n"
                + "        _mainSignals.Outgoing.SetCameraTarget\n"
                + "            .Connect(_cameraSignals.Incoming.SetCameraTarget);\n"
                + "\n"
                + "    private void OutGoingSignals() =>\n"
                + "        _cameraSignals.Outgoing.CameraTargetReady\n"
                + "            .Connect(_mainSignals.Incoming.CameraTargetReady);\n"
                + "}");

            painter.SubHeading("One entry on the Root");
            painter.Paragraph(
                "A sub-context is not found by reflection: the Root that owns it lists it. Select "
                + "your ConnectorRoot prefab and add CameraConnectorSubContext to Sub Context "
                + "Types. Without that line the class compiles and never runs.");

            painter.Space();
            painter.Note(
                "The signals on the other side are the game's, not the module's. MainSignals here "
                + "is only the example - whichever module knows who the camera should follow is "
                + "the one that declares SetCameraTarget and gets connected to it.");
        }
    }
}

#endif