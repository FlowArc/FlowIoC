#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The world pointer module: the two sides that meet in it - a world object registered on
    /// a channel, a screen registered as that channel's display - what happens at the edge of the frame,
    /// and the button that puts it in the project.
    /// </summary>
    internal class WorldPointerModulePage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string ModuleFolderName => "WorldPointerModule";

        public override string Title => "World Pointer Module";

        public override string Subtitle => "UI on a screen that follows a 3D object, and what it does at the edge of the frame";

        public override FlowIcon Icon => FlowIcon.Eye;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene; give the screen a layer and the layer an indicator prefab.",
                "The Root carries one status asset and nothing to author. What is drawn lives in a "
                + "screen of the module that owns the world objects."),
            new HelpTab("World side", DrawWorldSide,
                "Register a Transform on a channel and send it requests.",
                "The module that owns the object never sees a canvas. It says where and what, and "
                + "whether a screen is up to draw it is not its business."),
            new HelpTab("Screen side", DrawScreenSide,
                "Register a pool display as the channel's display while the screen is open.",
                "A Command registers the display when the screen has shown and takes it away when "
                + "the screen hides, because a Mediator may inject nothing but its View."),
            new HelpTab("Modes", DrawModes,
                "Hide, clamp to the edge, or ignore.",
                "The frame is the camera's pixel rect inset by margins the display sets, so a HUD "
                + "bar across the top is a margin rather than a special case.")
        };

        public override string InstalledHint =>
            "Drop WorldPointerServiceRoot into your scene, then register a WorldPointerPoolDisplay "
            + "from a screen of your own; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "An object in the world, and a screen that draws something over it.";

        public override string BodyTagline =>
            "An emote over a head, a health bar over a unit, a marker on a quest target: the module "
            + "that owns the object registers it on a channel, a screen registers as that channel's "
            + "display, and every frame the screen's indicator sits where the object is.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("WorldPointerOverTarget.png"),
                "WorldPointerTestScene in play: the sample screen's label stands over the cube it follows.");

            painter.SubHeading("What it gives you");
            painter.Bullet(
                "RegisterTarget takes a channel - what kind of pointer this is, such as Emote - and "
                + "the Transform to follow. SetContent, Show and Hide are requests on the same pair; "
                + "UnregisterTarget ends it. No handle is kept, and nothing is returned: a refused "
                + "call is an error naming its line.");
            painter.Bullet(
                "Every lookup is one step, keyed by channel and Transform, so a thousand targets "
                + "cost a register or a request no more than one does.");
            painter.Bullet(
                "RegisterDisplay makes a display the one display of a channel. While both sides "
                + "are registered every target has an indicator from the display; with no display "
                + "the targets wait, and their last content and last Show or Hide are replayed to "
                + "the display that comes.");
            painter.Bullet(
                "Outside the frame an indicator hides, stays pinned to the frame's edge with an "
                + "arrow aimed at its target, or carries on wherever the projection lands - "
                + "whichever the display's options say.");
            painter.Bullet(
                "The indicator is told its state - Hidden, InFrame, OnEdge - once when it starts "
                + "and then only when it changes, so it can fade rather than blink.");
            painter.Bullet(
                "RD_WorldPointer shows, during play, every channel with its display and its targets - "
                + "the answer to why nothing is shown is usually a row with no display.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.WorldPointer and injects "
                + "IWorldPointerService directly. It has no signals: nothing outside it needs "
                + "telling.");
            painter.Note(
                "Important: an overlay canvas exists only through the ScreenManager. The module "
                + "that owns the targets never creates UI or parents anything under a canvas of its "
                + "own - the indicators live in one of its screens, on a ScreenManager layer "
                + "the game has given a canvas of its own. The Setup tab shows how.");

            painter.SubHeading("How it places");
            painter.Paragraph(
                "One LateUpdate walks every matched target: it is projected through the camera, "
                + "judged against the display's frame, and the indicator is placed by world point "
                + "on its parent's plane - which is what makes any parent, any anchors and both "
                + "canvas modes come out right. Behind the camera Unity's projection is mirrored "
                + "through the centre, so it is mirrored back before anything reads it.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives "
                + "with it. Open WorldPointerTestScene under the test module's Scenes folder and "
                + "press Play: the camera orbits three cubes, the sample screen opens on Layer_3 - "
                + "which the scene's ScreenManager gives a canvas of its own - and "
                + "a screen of buttons on Layer_5, and Register points at each cube in one mode - "
                + "hide, clamp with an arrow, ignore. Change content, Hidden and Close / open screen "
                + "show the waiting and the replay. The scene has no canvas of its own: everything "
                + "it shows opens through the ScreenManager.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop WorldPointerServiceRoot from the module's Prefabs folder into the scene. It "
                + "ships at Initialize Order -30, in the Service band, with RD_WorldPointer filed "
                + "on its adapter.");

            painter.SubHeading("2. The camera");
            painter.Paragraph(
                "Pointers are projected through Camera.main until a game sets the service's Camera "
                + "property, and it is read again whenever it comes back null - a scene load that "
                + "replaced the camera is picked up on its own.");
            painter.Note(
                "Important: Camera.main is the camera tagged MainCamera. With no camera so tagged "
                + "and none set on the service, every indicator is told Hidden and nothing is "
                + "logged - there is nothing wrong to report, only nothing to project through.");

            painter.SubHeading("3. The screen");
            painter.Paragraph(
                "Create a screen module in the zScreenModules of the module that owns the objects, "
                + "on a layer set aside for UI that moves - Layer_3 or Layer_4 in the setup below.");

            painter.SubHeading("4. The ScreenManager (recommended)");
            painter.Paragraph(
                "In the scene, select Layer_3 and Layer_4 under the ScreenManager and add a Canvas "
                + "and a Graphic Raycaster to each. Leave Override Sorting off, so the layers still "
                + "draw in hierarchy order.");
            painter.Image(_images.Get("WorldPointerScreenManagerHierarchy.png"),
                "The ScreenManager in WorldPointerTestScene: Layer_3 selected.");
            painter.Image(_images.Get("WorldPointerLayerInspector.png"),
                "Layer_3 with the Canvas and the Graphic Raycaster added as overrides of the scene's instance.");
            painter.Paragraph(
                "Why: a canvas rebuilds all of its geometry whenever anything under it moves. "
                + "Indicators move every frame, so on the ScreenManager's one canvas they would "
                + "rebuild the HUD and every other screen with them, every frame. A layer with a "
                + "canvas of its own rebuilds only itself.");
            painter.Paragraph(
                "The shipped ScreenManager prefab is left as it is: the canvases are overrides on "
                + "the scene's instance, and which layers get one is the game's decision - a game "
                + "may give Layer_5 a canvas too, or use other layers entirely.");
            painter.Note(
                "Important: a layer's Canvas needs its own Graphic Raycaster. The ScreenManager's "
                + "raycaster does not reach into a nested canvas, so every button on that layer "
                + "stops taking clicks, and nothing reports it.");

            painter.SubHeading("5. The indicator and its pool");
            painter.Paragraph(
                "Derive one line of class: EmoteIndicator : WorldPointerIndicator<EmoteVO> goes on "
                + "the indicator prefab and fills its own Text or Image in SetContent. The indicator "
                + "is a PoolableItem, and its prefab is an item of a CD_PoolGroup like any other "
                + "pooled object, filed on the scene's PoolServiceRoot.");
            painter.Paragraph(
                "The screen's View holds where the indicators go and how they behave - a "
                + "RectTransform in the screen's prefab and a CD_WorldPointerOptions preset "
                + "(Create > FlowIoC > WorldPointerModule > Data). The register Command builds a "
                + "WorldPointerPoolDisplay from the pool, the item key and those two; the Screen side "
                + "tab shows it.");
            painter.Table(new[] {"WorldPointerIndicator field", "What it takes"},
                new[] {"Rect", "the element itself, unless another RectTransform is named"},
                new[] {"Arrow Pivot", "an empty RectTransform at the element's centre; the arrow image is its child"},
                new[] {"Canvas Group", "fade instead of SetActive"});
            painter.Note(
                "Important: the arrow image is authored pointing up. The pivot's local up is aimed "
                + "at the target, so an arrow drawn any other way points the wrong way, and nothing "
                + "reports it.");
            painter.Note(
                "Important: an item key no pool group lists, or a prefab that does not show the "
                + "display's content type, is reported the first time an indicator is asked for, "
                + "and nothing is drawn.");
            painter.Note(
                "Important: the display takes indicators with the pool's synchronous Get, so an "
                + "addressable indicator prefab has to be in the pool already - warm its group at "
                + "boot - or the Get is refused.");

            painter.SubHeading("6. The channel and the content");
            painter.Paragraph(
                "Both go in the Shared assembly of the module that owns the objects - a const "
                + "string and a [Serializable] value object. Its screen module reads them there, "
                + "the way a sub module reads its parent's published data.");
        }

        private void DrawWorldSide(HelpPainter painter)
        {
            painter.Code(
                "[Inject] private IWorldPointerService _worldPointer { get; set; }\n"
                + "\n"
                + "_worldPointer.RegisterTarget(EmoteIds.Emote, hanger.Head);\n"
                + "_worldPointer.SetContent(EmoteIds.Emote, hanger.Head, new EmoteVO {Kind = EmoteKind.Happy});\n"
                + "\n"
                + "// later, before the hanger goes back to its pool\n"
                + "_worldPointer.UnregisterTarget(EmoteIds.Emote, hanger.Head);");

            painter.Paragraph(
                "The pair is the key, so the Command that sends a request needs nothing but the "
                + "Transform it already holds. Registering the same pair twice is an error and the "
                + "second call is ignored; two pointers on one object use two channels.");
            painter.Note(
                "Important: a target the game pools is unregistered before it goes back. A "
                + "registration left alive keeps pointing at whatever uses that Transform next, and "
                + "nothing is logged, because the Transform is still there.");
            painter.Note(
                "Important: a request for a pair that was never registered is an error naming the "
                + "line that sent it. RegisterTarget comes first.");

            painter.SubHeading("Clearing everything");
            painter.Paragraph(
                "UnregisterAll unregisters every target and hands every indicator back to its "
                + "display; the displays stay. The sequence that ends a run binds the step the "
                + "module ships, .ToSequence<IWorldPointerService.Commands.UnregisterAll>().");
        }

        private void DrawScreenSide(HelpPainter painter)
        {
            painter.Code(
                "// the screen's Mediator: nothing decided, so it only dispatches\n"
                + "private void OnScreenShown(IScreenBody screen) => _signals.DisplayShown.Dispatch(_view);\n"
                + "private void OnScreenHidden(IScreenBody screen) => _signals.DisplayHidden.Dispatch();\n"
                + "\n"
                + "// RegisterEmoteDisplayCommand\n"
                + "[Inject] private IWorldPointerService _worldPointer { get; set; }\n"
                + "[Inject] private IPoolService _pool { get; set; }\n"
                + "\n"
                + "var display = new WorldPointerPoolDisplay<EmoteVO>(_pool, EmotePoolKeys.Bubble,\n"
                + "    _view.BubbleParent, _view.Options.Options);\n"
                + "_worldPointer.RegisterDisplay(EmoteIds.Emote, display);\n"
                + "\n"
                + "// UnregisterEmoteDisplayCommand\n"
                + "_worldPointer.UnregisterDisplay(EmoteIds.Emote);");

            painter.Paragraph(
                "A screen is pooled, so the pair runs on every opening: ShowCompleted registers, "
                + "HideCompleted takes it away. While the screen is closed the targets wait; when it "
                + "opens again they come back with their last content.");
            painter.Note(
                "Important: one channel has one display. A second RegisterDisplay for the same channel is "
                + "refused with an error naming its line, and so is a display whose content type "
                + "differs from content a target already holds.");

            painter.SubHeading("Letting an indicator go slowly");
            painter.Paragraph(
                "The service never tells an indicator Hidden when it hands it back - the display "
                + "decides how it goes. WorldPointerPoolDisplay hands it back through the indicator's "
                + "own Dismiss, which returns it to the pool at once; override Dismiss to play a fade "
                + "and call the base when the fade ends.");

            painter.SubHeading("Placing something once");
            painter.Paragraph(
                "A damage number does not follow; it appears where the hit was and animates itself. "
                + "The screen that draws it asks TryProject for the world point on one of its own "
                + "RectTransforms, and false when the position is behind the camera.");
            painter.Code(
                "if (_worldPointer.TryProject(hitPosition, _numbersParent, out Vector3 point))\n"
                + "    number.transform.position = point;");
        }

        private void DrawModes(HelpPainter painter)
        {
            painter.Table(new[] {"OffScreenMode", "Outside the frame", "States it is told"},
                new[] {"Hide", "Hidden, and behind the camera too; placed only while InFrame", "Hidden, InFrame"},
                new[] {"ClampToEdge", "pinned to the edge, on the ray from the frame's centre", "OnEdge, InFrame - never Hidden"},
                new[] {"Ignore", "placed wherever the projection lands", "InFrame once, and never again"});

            painter.Paragraph(
                "The mode and the rest of the options belong to the display, in the "
                + "CD_WorldPointerOptions preset it is built with. Two channels drawn differently are two displays with two "
                + "presets.");
            painter.Image(_images.Get("WorldPointerModes.png"),
                "The three modes in WorldPointerTestScene: Cube_InFrame in Hide (green), Cube_Behind in "
                + "Ignore (blue), and Cube_LeavesFrame in ClampToEdge (orange) - off to the lower right, "
                + "so its label is pinned to the edge with the arrow aimed at it.");

            painter.SubHeading("Hide");
            painter.Paragraph(
                "A health bar over a unit: it belongs over the unit and nowhere else. Outside the "
                + "frame the indicator is told Hidden and left where it was; back inside it is told "
                + "InFrame and placed again.");

            painter.SubHeading("Clamp to edge");
            painter.Paragraph(
                "A wave gathering at a gate the player cannot see: the indicator stays visible, "
                + "pinned to the frame's edge where the ray from the frame's centre towards the "
                + "target leaves it, and the arrow pivot's local up is turned along that ray. The "
                + "frame is first shrunk by the indicator's own half size, so the whole of it stays "
                + "on screen at the edge rather than half of it hanging off. The "
                + "ray starts at the frame's own centre rather than the screen's, so uneven margins "
                + "need no special case. Behind the camera the direction is flipped, so a target "
                + "behind and to the right is pointed at on the right.");
            painter.Image(_images.Get("WorldPointerClampToEdge.png"),
                "Pinned to the right edge: the arrow sits outside the panel, in the panel's tint, turned "
                + "towards the cube.");
            painter.Note(
                "Important: the frame is shrunk by the indicator's Rect, not by what is drawn. An arrow "
                + "that reaches outside the Rect hangs off the screen at the edge, and nothing reports "
                + "it. The sample's Rect is a square the arrow's whole turn fits in, with the panel a "
                + "child at its centre.");

            painter.SubHeading("Ignore");
            painter.Paragraph(
                "An element that manages its own visibility - it fades itself, or it is fine leaving "
                + "the screen. The service places it wherever the projection lands and never touches "
                + "its state after the first InFrame. Behind the camera it lands at the mirrored "
                + "point; if that matters, the mode is Hide.");

            painter.SubHeading("Offsets, margins, smoothing");
            painter.Paragraph(
                "World Offset lifts the point before projecting - the height of a head. Screen "
                + "Margins inset the frame: a HUD bar 160 px tall across the top is a top margin of "
                + "160. Smooth Speed above zero eases the indicator towards its place by "
                + "1 - exp(-speed * dt) each frame, and turns the arrow the same way; zero snaps.");

            painter.Space();
            painter.Note(
                "Indicators are placed after the camera moves: UpdateProvider runs its LateUpdate at "
                + "execution order 1000, behind CinemachineBrain's. A project that orders "
                + "UpdateProvider earlier by hand gets every indicator a frame behind the camera.");
        }
    }
}

#endif