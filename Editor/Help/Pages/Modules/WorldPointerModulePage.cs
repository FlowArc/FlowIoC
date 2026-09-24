#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The world pointer module: the two sides that meet in it - a world object registered under
    /// an id, a screen registered as that id's display - what happens at the edge of the frame,
    /// and the button that puts it in the project.
    /// </summary>
    internal class WorldPointerModulePage : ModulePage
    {
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
                "Register a Transform under an id and send it requests.",
                "The module that owns the object never sees a canvas. It says where and what, and "
                + "whether a screen is up to draw it is not its business."),
            new HelpTab("Screen side", DrawScreenSide,
                "Register the screen's layer as the id's display while the screen is open.",
                "A Command registers the layer when the screen has shown and takes it away when "
                + "the screen hides, because a Mediator may inject nothing but its View."),
            new HelpTab("Modes", DrawModes,
                "Hide, clamp to the edge, or ignore.",
                "The frame is the camera's pixel rect inset by margins the display sets, so a HUD "
                + "bar across the top is a margin rather than a special case.")
        };

        public override string InstalledHint =>
            "Drop WorldPointerServiceRoot into your scene, then give a screen of your own a "
            + "WorldPointerLayer; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "An object in the world, and a screen that draws something over it.";

        public override string BodyTagline =>
            "An emote over a head, a health bar over a unit, a marker on a quest target: the module "
            + "that owns the object registers it under an id, a screen registers as that id's "
            + "display, and every frame the screen's indicator sits where the object is.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "RegisterTarget takes an id and the Transform to follow. SetContent, Show and Hide "
                + "are requests on the same pair; UnregisterTarget ends it. No handle is kept.");
            painter.Bullet(
                "RegisterDisplay makes a screen's layer the one display of an id. While both sides "
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
                "RD_WorldPointer shows, during play, every id with its display and its targets - "
                + "the answer to why nothing is shown is usually a row with no display.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.WorldPointer and injects "
                + "IWorldPointerService directly. It has no signals: nothing outside it needs "
                + "telling.");
            painter.Note(
                "Important: an overlay canvas exists only through the ScreenManager. The module "
                + "that owns the targets never creates UI or parents anything under a canvas of its "
                + "own - the indicators live in one of its screens, on the ScreenManager's "
                + "own-canvas layers, Layer_3 and Layer_4.");

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
                + "press Play: the camera orbits three cubes, the sample screen opens on Layer_3 and "
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
                + "on Layer_3 or Layer_4. Those two layers sit under a canvas of their own inside "
                + "the ScreenManager, so indicators moving every frame never rebuild the HUD's "
                + "canvas.");

            painter.SubHeading("4. The layer and the indicator");
            painter.Paragraph(
                "Derive one line of class for each: EmoteLayer : WorldPointerLayer<EmoteVO> goes on "
                + "an object in the screen's prefab, EmoteIndicator : WorldPointerIndicator<EmoteVO> "
                + "on the indicator prefab, and fills its own Text or Image in SetContent.");
            painter.Table(new[] {"WorldPointerLayer field", "What it takes"},
                new[] {"Prefab", "the indicator prefab; it must show the layer's content type"},
                new[] {"Parent", "where indicators are parented - the layer's own RectTransform unless named"},
                new[] {"Options", "a CD_WorldPointerOptions preset - Create > FlowIoC > WorldPointerModule > Data"});
            painter.Table(new[] {"WorldPointerIndicator field", "What it takes"},
                new[] {"Rect", "the element itself, unless another RectTransform is named"},
                new[] {"Arrow Pivot", "an empty RectTransform at the element's centre; the arrow image is its child"},
                new[] {"Canvas Group", "fade instead of SetActive"});
            painter.Note(
                "Important: the arrow image is authored pointing up. The pivot's local up is aimed "
                + "at the target, so an arrow drawn any other way points the wrong way, and nothing "
                + "reports it.");
            painter.Note(
                "Important: a layer whose prefab does not show its content type is reported the "
                + "first time it is asked for an indicator, and nothing is drawn.");

            painter.SubHeading("5. The id and the content");
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
                + "Transform it already holds. The same Transform twice under one id is refused; "
                + "two pointers on one object use two ids.");
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
                + "private void OnScreenShown(IScreenBody screen) => _signals.DisplayShown.Dispatch(_view.EmoteLayer);\n"
                + "private void OnScreenHidden(IScreenBody screen) => _signals.DisplayHidden.Dispatch(_view.EmoteLayer);\n"
                + "\n"
                + "// RegisterEmoteDisplayCommand\n"
                + "_worldPointer.RegisterDisplay(EmoteIds.Emote, _layer);\n"
                + "\n"
                + "// UnregisterEmoteDisplayCommand\n"
                + "_worldPointer.UnregisterDisplay(_layer);");

            painter.Paragraph(
                "A screen is pooled, so the pair runs on every opening: ShowCompleted registers, "
                + "HideCompleted takes it away. While the screen is closed the targets wait; when it "
                + "opens again they come back with their last content.");
            painter.Note(
                "Important: one id has one display. A second RegisterDisplay for the same id is "
                + "refused with an error naming its line, and so is a display whose content type "
                + "differs from content a target already holds.");

            painter.SubHeading("Letting an indicator go slowly");
            painter.Paragraph(
                "The service never tells an indicator Hidden when it hands it back - the display "
                + "decides how it goes. WorldPointerLayer.Release returns it to the pool at once; "
                + "override it to play a fade and call ReturnToPool when the fade ends.");

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
            painter.Code(
                "OffScreenMode.Hide          Hidden outside the frame or behind the camera; placed only while InFrame\n"
                + "OffScreenMode.ClampToEdge   never Hidden; OnEdge outside the frame, on the ray from the frame's centre\n"
                + "OffScreenMode.Ignore        placed wherever the projection lands; told InFrame once and never again");

            painter.Paragraph(
                "The mode and the rest of the options belong to the display, in the layer's "
                + "CD_WorldPointerOptions preset. Two ids drawn differently are two layers with two "
                + "presets.");

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