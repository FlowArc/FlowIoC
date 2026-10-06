#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The camera module: the cameras it names, how a game hands its own Cinemachine cameras
    /// over, the Service a game calls, and the button that puts it in the project.
    ///
    /// Unlike the counter module this module has packages behind it, so installing it may have
    /// to add them first - RequiredPackages is what the adapter asks about before it copies.
    /// </summary>
    internal class CameraModulePage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string ModuleFolderName => "CameraModule";

        /// <summary>
        /// What the module's two assemblies reference. Cinemachine is the module's subject;
        /// the render pipeline core is where SerializedDictionary comes from, which is how a
        /// camera adapter maps a name to its configuration in the Inspector.
        /// </summary>
        public override IReadOnlyList<string> RequiredPackages => new[]
        {
            "com.unity.cinemachine",
            "com.unity.render-pipelines.core"
        };

        public override string Title => "Camera Module";

        public override string Subtitle => "Named Cinemachine cameras";

        public override FlowIcon Icon => FlowIcon.Camera;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Usage", DrawUsage,
                "Put CameraServiceRoot in the scene, then list its cameras on the one adapter.",
                "Every camera sits under the Root's CameraManager and is listed on its "
                + "CameraAdapterView. They register on their own when the scene loads - there is no "
                + "call to make.")
        };

        public override string BodyHeadline => "Cinemachine cameras get names, and switching is one call.";

        public override string BodyTagline =>
            "A menu camera and a gameplay camera come with the module; a game that needs more adds "
            + "them to one enum.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A camera is a CameraName, not a scene reference. Switching is one call or one step, "
                + "and nothing that switches has to know which GameObject the camera sits on.");
            painter.Bullet(
                "Cameras register themselves. An adapter on the rig hands its cameras over when "
                + "the scene loads and takes them back when it unloads, so a scene change does not "
                + "leave the model holding cameras that are gone.");
            painter.Bullet(
                "It moves the camera and remembers where it was. MoveCamera glides the live camera to "
                + "a point; RememberPosition stores its position under a camera's name and "
                + "TryGetRememberedPosition gives it back - what a game needs when the player comes "
                + "back from a menu.");
            painter.Bullet(
                "Custom blends are data. CD_CameraCustomBlends holds the Cinemachine blend table, "
                + "and a camera's own entry can override it as it registers.");

            painter.Space();
            painter.Note(
                "It is a Service, and ICameraService is the only way in: the module has no public "
                + "signals and needs no Connector. A module of yours references Modules.Camera, "
                + "injects the Service and calls it, or binds one of its steps in a sequence.");

            painter.Separator();
            painter.SubHeading("What lands in the project");
            painter.Table(new[] {"What", "Holds"},
                new[] {"Modules.Camera", "ICameraService and its steps, the model, the commands, the adapters."},
                new[]
                {
                    "Modules.Camera.Shared",
                    "The CameraName enum and CameraCVO on their own, so a module that names a camera "
                    + "references the data and not the module."
                },
                new[] {"Prefabs/CameraServiceRoot", "The module's presence in the scene."},
                new[] {"Scriptables/CD_CameraCustomBlends", "The blend table."});

            painter.Separator();
            painter.SubHeading("What it needs");
            painter.Paragraph(
                "com.unity.cinemachine, and com.unity.render-pipelines.core for the "
                + "SerializedDictionary the multi-camera adapter is authored through. The Install "
                + "button checks both and offers to add whichever is absent before it copies "
                + "anything.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("The Root");
            painter.Paragraph(
                "Drop CameraServiceRoot into the scene, in the Services band at Initialize Order -40. "
                + "It brings its own Main Camera with the Cinemachine brain, and a CameraManager - the "
                + "adapter view - holding a Cinemachine camera and the target it follows.");
            painter.Image(_images.Get("CameraRootHierarchy.png"),
                "The Root in a scene, opened out.");
            painter.Image(_images.Get("CameraRootAdapter.png"),
                "Its adapter as it ships: CD_CameraCustomBlends, the blend table, in the Scriptable Map.");

            painter.Separator();
            painter.SubHeading("Adding a camera");
            painter.Paragraph(
                "All of a scene's cameras live in one rig. Never give a camera a view of its own: the "
                + "CameraManager's adapter is the one view, and it registers every camera it lists.");
            painter.Bullet("Add a CinemachineCamera under CameraManager, and an empty target beside it for the camera to follow and look at.");
            painter.Bullet("Give the camera a name in CameraName, with the next free number.");
            painter.Bullet("Add a row to the adapter's Camera Configs: the name, the camera, and Activate At Register on the one camera the scene starts on.");
            painter.Paragraph(
                "Because each camera follows a target of its own, MoveCamera moves one camera without "
                + "dragging the others, and the camera never follows a gameplay object directly unless "
                + "Follow points it at one.");

            painter.Separator();
            painter.SubHeading("Naming a camera");
            painter.Paragraph(
                "CameraName is the module's vocabulary and lives in its Shared assembly. Add the "
                + "entries the game needs and fill the adapter's map in the Inspector. Give each the "
                + "next free number: Unity stores a camera name as its number, so a name slipped in "
                + "between two others moves every camera below it.");
            painter.Code(
                "public enum CameraName\n"
                + "{\n"
                + "    Menu = 0,\n"
                + "    Gameplay = 1,\n"
                + "    Cutscene = 2\n"
                + "}");
            painter.Note(
                "The file is yours to extend - the module's card says so on its Extend line. An "
                + "update that changes it too asks before it touches your copy, the way it asks about "
                + "a data asset you edited.");

            painter.Separator();
            painter.SubHeading("Switching");
            painter.Paragraph(
                "A switch that is one fixed step of a flow is bound as the module's own step, with "
                + "the camera beside it, so the flow reads from the Context.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.LevelStarted)\n"
                + "    .ToSequence<PrepareLevelCommand>()\n"
                + "    .ToSequence<ICameraService.Commands.Switch>(CameraName.Gameplay);");
            painter.Paragraph(
                "Where a Command decides which camera goes live, or what it follows, it injects the "
                + "Service.");
            painter.Code(
                "[Inject] private ICameraService _cameras { get; set; }\n"
                + "\n"
                + "_cameras.Switch(CameraName.Gameplay);\n"
                + "_cameras.Follow(_playerTransform);");

            painter.Separator();
            painter.SubHeading("Moving the camera");
            painter.Paragraph(
                "MoveCamera moves what the live camera follows to a point over the seconds given, so "
                + "the camera glides there. As a step it holds the sequence until the camera arrives; "
                + "called directly it takes a callback for the arrival.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.BossAppeared)\n"
                + "    .ToSequence<ICameraService.Commands.MoveCamera>(new Vector3(0f, 12f, -8f), 0.6f)\n"
                + "    .ToSequence<ShowBossIntroCommand>();\n"
                + "\n"
                + "_cameras.MoveCamera(_boss.position, 0.6f, () => FlowLogger.Log(\"Camera on the boss.\"));");

            painter.Separator();
            painter.SubHeading("Zooming");
            painter.Paragraph(
                "SetDistance eases the live camera's distance from what it follows - its position "
                + "composer's Camera Distance - to the one given, over the seconds given. As a step it "
                + "holds the sequence until the zoom ends.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.AimStarted)\n"
                + "    .ToSequence<ICameraService.Commands.SetDistance>(6f, 0.4f);\n"
                + "\n"
                + "_cameras.SetDistance(14f, 0.4f);");

            painter.Separator();
            painter.SubHeading("Remembering where a camera was");
            painter.Paragraph(
                "Remember the live camera's position before the flow leaves it, and move back to it "
                + "when the flow returns.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.MenuOpened)\n"
                + "    .ToSequence<ICameraService.Commands.RememberPosition>(CameraName.Gameplay)\n"
                + "    .ToSequence<ICameraService.Commands.Switch>(CameraName.Menu);\n"
                + "\n"
                + "if (_cameras.TryGetRememberedPosition(CameraName.Gameplay, out Vector3 position))\n"
                + "    _cameras.MoveCamera(position, 0.4f);");

            painter.Separator();
            painter.SubHeading("The test scene");
            painter.Paragraph(
                "CameraTestScene, in the module's test module, shows all of it: two cubes, a camera on "
                + "each with a target of its own. Switch camera goes from one to the other; Move camera "
                + "glides the live one to a random point around its cube, which the scene remembered "
                + "at launch; Zoom camera eases it to a random distance.");
        }
    }
}

#endif