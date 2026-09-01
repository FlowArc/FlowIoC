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
                + "This window is the catalogue of those configs, and you edit them in the table "
                + "rather than opening one Inspector at a time.");

            painter.SubHeading("Editing");
            painter.Paragraph(
                "Every cell writes straight to the asset. Change a value and press Enter and it is "
                + "saved, Ctrl+Z takes it back, and the same validation the Inspector runs still "
                + "reports a config that no longer makes sense.");

            painter.SubHeading("The columns");
            painter.Bullet("Layer - the layer the screen opens in unless the call overrides it.");
            painter.Bullet("Load Type - Addressable, Resource or DirectPrefab.");
            painter.Bullet("Tag - the group the screen belongs to, for opening or closing a set at once.");
            painter.Bullet("Resource Path, Addressable Key, Direct Prefab - one per load type; fill in the one the Load Type names.");
            painter.Bullet("Show and Hide - whether the screen animates on the way in and on the way out.");
            painter.Bullet("View Type and Mediator Type - hidden by default. Right click the header to show them.");

            painter.Paragraph(
                "Right click a header to hide a column you do not use, and drag the edge to resize "
                + "one. Click a header to sort by it. The layout, the search text and the three "
                + "filters are remembered between sessions.");

            painter.SubHeading("Finding a config");
            painter.Paragraph(
                "The search box matches the config's name as well as its resource path, addressable "
                + "key and prefab name. The Load, Tag and Layer filters narrow the list together "
                + "rather than one at a time, and the count on the right reads shown over total.");

            painter.SubHeading("Where configs come from");
            painter.Paragraph(
                "Create Module writes the config when you generate a Screen module, and that is the "
                + "only place they are made. This window is where you review and adjust what the "
                + "generator produced. Right click a row to ping it in the Project window, select "
                + "it, reveal it on disk, or delete it.");
        }
    }
}

#endif