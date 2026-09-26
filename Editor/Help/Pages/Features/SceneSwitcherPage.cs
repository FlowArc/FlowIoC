#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Features
{
    internal class SceneSwitcherPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public SceneSwitcherPage() : base(null)
        {
        }

        public override string Title => "Scene Switcher";

        public override FlowIcon Icon => FlowIcon.Layers;

        protected override string BodyHeadline => "Every scene the modules bring, one click from the toolbar.";

        protected override string BodyTagline =>
            "A dropdown on Unity's main toolbar that lists every scene under Assets/Modules - the "
            + "game's own, each module's test scene, each screen's - and opens the one you pick, in "
            + "edit mode and in play mode.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("SceneSwitcherPopup.png"),
                "The Scene Switcher dropped down from the main toolbar.");

            painter.SubHeading("Turning it on");
            painter.Paragraph(
                "When the dropdown is not on the toolbar, right-click an empty part of the main "
                + "toolbar, open FlowIoC and tick Scene Switcher. It appears on the right of the "
                + "toolbar and stays there, because Unity remembers the choice.");
            painter.Image(_images.Get("SceneSwitcherToolbarMenu.png"),
                "Right-click the main toolbar > FlowIoC > Scene Switcher. Show All and Hide All turn "
                + "every FlowIoC element on the toolbar on or off at once.");

            painter.Note(
                "The main toolbar takes elements of its own from Unity 6.3 onwards, so the Scene "
                + "Switcher is there from Unity 6.3 and absent in an older Editor.");

            painter.SubHeading("What the list shows");
            painter.Bullet(
                "The game's own scenes - MainScene and any other scene in a module's Scenes folder - "
                + "pinned above every tab, because everybody looks for them.");
            painter.Bullet(
                "Frequent: the scenes you open most, most first. It is your habit rather than the "
                + "project's, so it is kept in EditorPrefs and differs from one developer to the next.");
            painter.Bullet(
                "Modules: every scene, under the module it belongs to. A module with more than one "
                + "scene folds open and stays the way you left it.");
            painter.Bullet("Screens: the test scene of every screen module.");
            painter.Bullet("Tests: the scene of every test module.");
            painter.Paragraph(
                "Typing in the search looks through every scene whatever the tab says, so a scene is "
                + "never missed for sitting on another tab. A heading and its rows wear the colour "
                + "the module's Root wears in the inspector - Service, System, Core.");

            painter.SubHeading("In play mode");
            painter.Paragraph(
                "Picking a scene while playing loads it through SceneManager, which reaches only the "
                + "scenes in Build Settings. A scene that is not there is reported in the console "
                + "rather than opened.");
        }
    }
}

#endif
