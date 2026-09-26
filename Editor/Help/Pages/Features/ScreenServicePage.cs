#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Features
{
    internal class ScreenServicePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public ScreenServicePage() : base(null)
        {
        }

        public override string Title => "Screen Service";

        public override FlowIcon Icon => FlowIcon.ScreenStack;

        protected override string BodyTabTitle => "Setup";

        protected override string BodyHeadline => "Every screen opens through one service, from a Command.";

        protected override string BodyTagline =>
            "A screen is a module of its own: a context that declares it, a view and a mediator. The "
            + "screen service loads it, pools it and puts it on a layer; the module that shows it lists "
            + "the screen's context on its Root and opens it from a Command.";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Config", DrawConfig, "Listed on the Root that shows it.",
                "The screen's context declares where it opens; the Root that lists it may place it "
                + "somewhere else for itself."),
            new HelpTab("Usage", DrawUsage, "Opened from a Command.",
                "A screen opens as a step of a flow, so the Flow Console shows it.")
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("Three pieces in the scene");
            painter.Bullet("ScreenServiceRoot binds IScreenService, with the other services at the top.");
            painter.Bullet(
                "ScreenRoot carries the ScreenManager and its layers - the canvas every screen is drawn "
                + "on. The setup set ships it ready.");
            painter.Bullet(
                "The Root of each module that shows a screen lists that screen's context. A screen has "
                + "no Root of its own.");
            painter.Image(_images.Get("ScreenRootHierarchy.png"),
                "MainScene: ScreenServiceRoot among the services, ScreenRoot with its manager and ten "
                + "layers below the game's own Roots.");
            painter.PageLink("Screen Module", "Read: Screen Module - the manager, its layers and tags");
        }

        private void DrawConfig(HelpPainter painter)
        {
            painter.SubHeading("The entry on the Root");
            painter.Paragraph(
                "Add Sub Context on the module's Root offers every screen context; the SCREEN badge marks "
                + "one. Opened, the entry shows how the screen is configured - where it loads from, its "
                + "manager, layer and tag - as the context declares it.");
            painter.Image(_images.Get("ScreenSubContextInspector.png"),
                "WorldPointerTestRoot lists the World Pointer sample's two screens. The sample screen "
                + "loads from Resources and opens on layer 3.");

            painter.Separator();

            painter.SubHeading("Overriding it for one Root");
            painter.Paragraph(
                "Override Screen lets this one Root place the screen somewhere else - another manager, "
                + "layer or tag, or the animations turned on - without touching the context. Where the "
                + "prefab loads from is the module's and is never overridden.");
            painter.Paragraph(
                "The Screen Scanner lists every screen of the open scenes and edits the same values in "
                + "one table.");
            painter.PageLink("Screen Scanner", "Open: Screen Scanner");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("Opening a screen");
            painter.Code(
                "[Inject] private IScreenService _screenService { get; set; }\n"
                + "\n"
                + "GameplayScreenView screen = await _screenService.Open<GameplayScreenView>().Show<GameplayScreenView>();",
                "Open returns a builder; nothing happens until Show, which can answer null");
            painter.Paragraph(
                "The screen is registered with the service in the Setup of the Root that lists it, so "
                + "any Launch step can open it. A screen whose Root is not in the scene is not "
                + "registered, and Open says so.");
            painter.PageLink("Screen Module", "Read: Screen Module - the context, opening and filling a screen");
        }
    }
}

#endif
