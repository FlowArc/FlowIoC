#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ScreenConfigManagerPage : HelpPage
    {
        public ScreenConfigManagerPage() : base(null)
        {
        }

        public override string Title => "Screen Config Manager";

        public override string Icon => "Canvas Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Screen Config Manager. Every screen in the game has a config "
                + "asset saying where its prefab comes from and how the screen behaves by default. "
                + "This window is the catalogue of those configs in one place, rather than one "
                + "Inspector at a time.");

            painter.SubHeading("What a screen config holds");
            painter.Bullet("LoadType - Addressable, Resource or DirectPrefab.");
            painter.Bullet("The address, resource path or prefab reference that goes with that load type.");
            painter.Bullet("DefaultLayer - the layer the screen opens in unless the call overrides it.");

            painter.Paragraph(
                "Create Module writes the config for you when you generate a Screen module, so in "
                + "day to day work this window is where you review and adjust what the generator "
                + "produced.");
        }
    }
}

#endif
