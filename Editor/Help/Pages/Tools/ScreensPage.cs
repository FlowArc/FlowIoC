#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ScreensPage : HelpPage
    {
        public ScreensPage() : base(null)
        {
        }

        public override string Title => "Screens";

        public override string Icon => "Canvas Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Screens. Every screen context attached to a Root in the open "
                + "scenes, grouped by the ScreenManager it registers at. It answers the question a "
                + "single Root's inspector cannot: which layer does each screen open in, and do any "
                + "two of them want the same one.");

            painter.SubHeading("Layer collisions");
            painter.Paragraph(
                "Two screens on one layer of one manager are tinted red. This is advice, not an "
                + "error: a screen opening on an occupied layer closes the one already there, which "
                + "is how a stack of full screen views is meant to behave. The tint says it will "
                + "happen, so it happens on purpose rather than by surprise.");

            painter.SubHeading("Editing");
            painter.Paragraph(
                "Layer, manager, tag and the two animation flags are editable in place. A screen "
                + "declares those in its context's ScreenCVO, which is code, so an edit here does "
                + "what ticking Override Screen in the Root's inspector does: it turns that entry's "
                + "override on, seeded from the declaration, and writes the new value there. The "
                + "row's name gains a star, and Reset drops the override and takes the declaration "
                + "back.");

            painter.SubHeading("What is not here");
            painter.Bullet(
                "Load. Where a prefab lives is the module's business, not the scene's, so no window "
                + "repoints a screen at another address.");
            painter.Bullet(
                "Roots that are not in an open scene. A layer collision is a question about one "
                + "scene: two screens that are never in a scene together cannot clash.");

            painter.Paragraph(
                "A Root open in prefab stage is in a scene of its own, so its screens are listed "
                + "too, and editing one writes into the prefab.");
        }
    }
}

#endif
