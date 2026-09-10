#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The world pointer module: what it moves, how a game registers a pointer, what happens at
    /// the edge of the frame, and the button that puts it in the project.
    /// </summary>
    internal class WorldPointerModulePage : HelpPage
    {
        private const string ModuleFolderName = "WorldPointerModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public WorldPointerModulePage() : base(null)
        {
            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "World Pointer";

        public override string Subtitle => "UI that follows a 3D object, and what it does at the edge of the frame";

        public override FlowIcon Icon => FlowIcon.Eye;

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene and give your prefab the component.",
                "The Root carries nothing to author. The element that follows an object sits under "
                + "any Canvas, with WorldPointerIndicator on it and a preset if it wants one."),
            new HelpTab("Usage", DrawUsage,
                "Register a target and an indicator; dispose the handle to stop.",
                "One call starts a pointer, and the handle it hands back ends it. A Command does the "
                + "registering, because a Mediator may inject nothing but its View."),
            new HelpTab("Modes", DrawModes,
                "Hide, clamp to the edge, or ignore.",
                "The frame is the camera's pixel rect inset by margins you set, so a HUD bar across "
                + "the top is a margin rather than a special case.")
        };

        /// <summary>
        /// Whether the module is in the project, answered from a cache that goes stale after a
        /// second. The underlying check walks every asmdef under Assets, and the banner asks twice
        /// per repaint - often enough that doing the walk each time would cost real frames.
        /// </summary>
        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        private void Install()
        {
            // Whatever happened, what the cache holds is now a guess about a project that has
            // changed underneath it.
            _checkedAt = double.NegativeInfinity;

            if (_installer.TryInstall(ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog(
                    "World Pointer installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "It is yours to edit from here - the copy in the package is only the one "
                    + "installs are made from. Drop WorldPointerServiceRoot into your scene and put "
                    + "WorldPointerIndicator on the element that should follow; the Setup tab has the steps.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("World Pointer", error, "OK");
        }

        protected override string BodyHeadline =>
            "A UI element sits where a 3D object is on screen, every frame.";

        protected override string BodyTagline =>
            "A health bar over a unit, a name over a player, a \"wave incoming\" notice over a gate, "
            + "a marker on a quest target - each is one Register call, and the module decides what "
            + "happens when the object leaves the frame.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "Register takes the Transform to follow and the element to move, and hands back a "
                + "handle. Dispose the handle, or hand it to Unregister, and the pointer stops.");
            painter.Bullet(
                "Outside the frame a pointer does one of three things: hides, stays pinned to the "
                + "frame's edge on the ray towards its target with an arrow aimed at it, or carries "
                + "on wherever the projection lands.");
            painter.Bullet(
                "The indicator is told its state - Hidden, InFrame, OnEdge - once when it starts "
                + "and then only when it changes, so it can fade rather than blink.");
            painter.Bullet(
                "A destroyed target drops its own pointer. Nothing throws, and the indicator is told "
                + "Hidden once.");
            painter.Bullet(
                "TryProject answers a one-shot point on a canvas under a world position, for a "
                + "damage number placed once rather than followed.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.WorldPointer and injects "
                + "IWorldPointerService directly. It has no signals: nothing outside it needs "
                + "telling, and a game that wants pointers cleared when a run ends calls "
                + "UnregisterAll from a Command of its own.");

            painter.SubHeading("How it places");
            painter.Paragraph(
                "One LateUpdate walks every pointer: the target is projected through the camera, "
                + "judged against the frame, and the element is placed by world point on its "
                + "parent's plane - which is what makes any parent, any anchors and both canvas "
                + "modes come out right. Behind the camera Unity's projection is mirrored through "
                + "the centre, so it is mirrored back before anything reads it.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives "
                + "with it. Open WorldPointerTestScene under the test module's Scenes folder and "
                + "press Play: the camera orbits three cubes, and Register puts one pointer of each "
                + "mode over them - green hides, orange clamps with an arrow, blue ignores.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop WorldPointerServiceRoot from the module's Prefabs folder into the scene. It "
                + "ships at Initialize Order -5, in the Service band; nothing depends on where it "
                + "sits, because it has no Setup work and reaches no other module.");

            painter.SubHeading("2. The camera");
            painter.Paragraph(
                "Pointers are projected through Camera.main until a game sets the service's Camera "
                + "property, and it is read again whenever it comes back null - a scene load that "
                + "replaced the camera is picked up on its own.");
            painter.Note(
                "Important: Camera.main is the camera tagged MainCamera. With no camera so tagged "
                + "and none set on the service, every pointer is told Hidden and nothing is logged - "
                + "there is nothing wrong to report, only nothing to project through.");

            painter.SubHeading("3. The indicator");
            painter.Paragraph(
                "The element that follows an object is any RectTransform under a Canvas. Put "
                + "WorldPointerIndicator on it: it holds the RectTransform, an optional arrow pivot, "
                + "an optional CanvasGroup, and an optional CD_WorldPointerOptions preset. Hidden "
                + "fades the CanvasGroup to nothing when there is one and deactivates the object "
                + "when there is not; the arrow shows only on the edge.");
            painter.Code(
                "WorldPointerIndicator\n"
                + "  Rect          the element itself, unless another RectTransform is named\n"
                + "  Arrow Pivot   an empty RectTransform at the element's centre; the arrow image is its child\n"
                + "  Canvas Group  fade instead of SetActive\n"
                + "  Options       a CD_WorldPointerOptions preset - Create > FlowIoC > WorldPointerModule > Data");
            painter.Note(
                "Important: the arrow image is authored pointing up. The pivot's local up is aimed "
                + "at the target, so an arrow drawn any other way points the wrong way, and nothing "
                + "reports it.");
            painter.Note(
                "Important: an indicator with no Canvas above it is refused at Register with an "
                + "error naming the element and the line that registered it. An indicator that is "
                + "inactive at that moment is fine - a pooled one usually is.");

            painter.SubHeading("4. The frame");
            painter.Paragraph(
                "A preset's Screen Margins inset the frame from the camera's pixel rect. A HUD bar "
                + "160 px tall across the top is a top margin of 160: a target under the bar counts "
                + "as off screen, and a clamped pointer stops below it.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.Code(
                "[Inject] private IWorldPointerService _worldPointer { get; set; }\n"
                + "\n"
                + "WorldPointerHandle handle = _worldPointer.Register(unit.transform, indicator, indicator.Options);\n"
                + "\n"
                + "// later, when the unit dies or the element goes back to its pool\n"
                + "handle.Dispose();");

            painter.Paragraph(
                "Options may come from the indicator's preset, as above, be built inline, or be "
                + "left null for the defaults - no offsets, Hide, no margins, snap. A copy of the "
                + "handle disposed twice is harmless; a handle that was never valid does nothing.");
            painter.Code(
                "var options = new WorldPointerOptionsCVO\n"
                + "{\n"
                + "    WorldOffset   = new Vector3(0f, 2f, 0f),   // above the head\n"
                + "    OffScreen     = OffScreenMode.ClampToEdge,\n"
                + "    ScreenMargins = new RectOffset(24, 24, 160, 24),\n"
                + "    RotateArrow   = true,\n"
                + "    SmoothSpeed   = 12f\n"
                + "};");

            painter.SubHeading("Where the call goes");
            painter.Paragraph(
                "Registering is a Command's job. A Mediator may inject nothing but its View, so a "
                + "screen that shows a pointer dispatches - the target and the element as the "
                + "payload - and a Command in its module injects the service and registers. That is "
                + "exactly what the test module does with its two buttons.");
            painter.Note(
                "Important: an element the game pools must dispose its handle when it goes back - "
                + "OnReturnToPool is the place. A handle left alive keeps a pooled element following "
                + "an object it no longer belongs to, and nothing is logged, because the target is "
                + "still there.");

            painter.SubHeading("Placing something once");
            painter.Paragraph(
                "A damage number does not follow; it appears where the hit was and animates itself. "
                + "TryProject answers the world point on a canvas parent under a world position, and "
                + "false when the position is behind the camera.");
            painter.Code(
                "if (_worldPointer.TryProject(hitPosition, _numbersParent, out Vector3 point))\n"
                + "    number.transform.position = point;");

            painter.SubHeading("Clearing everything");
            painter.Paragraph(
                "UnregisterAll stops every pointer and tells each indicator Hidden - the end of a "
                + "run, or a scene about to unload. Bind it into the sequence that ends the run "
                + "through a one-line Command of your own; the module has no signal for it because "
                + "nothing outside it needs telling.");
        }

        private void DrawModes(HelpPainter painter)
        {
            painter.Code(
                "OffScreenMode.Hide          Hidden outside the frame or behind the camera; placed only while InFrame\n"
                + "OffScreenMode.ClampToEdge   never Hidden; OnEdge outside the frame, on the ray from the frame's centre\n"
                + "OffScreenMode.Ignore        placed wherever the projection lands; told InFrame once and never again");

            painter.SubHeading("Hide");
            painter.Paragraph(
                "A health bar over a unit: it belongs over the unit and nowhere else. Outside the "
                + "frame the indicator is told Hidden and left where it was; back inside it is told "
                + "InFrame and placed again.");

            painter.SubHeading("Clamp to edge");
            painter.Paragraph(
                "A wave gathering at a gate the player cannot see: the pointer stays visible, pinned "
                + "to the frame's edge where the ray from the frame's centre towards the target "
                + "leaves it, and the arrow pivot's local up is turned along that ray. The ray starts "
                + "at the frame's own centre rather than the screen's, so uneven margins need no "
                + "special case. Behind the camera the direction is flipped, so a target behind and "
                + "to the right is pointed at on the right.");

            painter.SubHeading("Ignore");
            painter.Paragraph(
                "An element that manages its own visibility - it fades itself, or it is fine leaving "
                + "the screen. The service places it wherever the projection lands and never touches "
                + "its state after the first InFrame. Behind the camera it lands at the mirrored "
                + "point; if that matters, the mode is Hide.");

            painter.SubHeading("Smoothing");
            painter.Paragraph(
                "SmoothSpeed above zero eases the element towards its place by 1 - exp(-speed * dt) "
                + "each frame, and turns the arrow the same way. Zero snaps, which is what a bar "
                + "over a moving unit wants; a clamped marker at the edge reads better eased.");

            painter.Space();
            painter.Note(
                "Pointers are placed after the camera moves: UpdateProvider runs its LateUpdate at "
                + "execution order 1000, behind CinemachineBrain's. A project that orders "
                + "UpdateProvider earlier by hand gets every pointer a frame behind the camera.");
        }
    }
}

#endif
