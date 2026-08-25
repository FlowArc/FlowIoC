#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class CodeGeneratorsPage : HelpPage
    {
        public CodeGeneratorsPage() : base(null)
        {
        }

        public override string Title => "Code Generators";

        public override string Icon => "cs Script Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "The generators write the shape the rest of the tooling expects. Use them rather "
                + "than copying a folder: a copied module carries the original's namespace and its "
                + "assembly name, and Unity refuses the resulting duplicate assembly name rather "
                + "than telling you which copy is at fault.");

            painter.SubHeading("Create Module");
            painter.Paragraph("Tools > FlowIoC > Create Module. The one you reach for first.");
            painter.Bullet("Main - a normal feature module under Assets/Modules/<Name>Module/.");
            painter.Bullet("Screen - a screen module: view, mediator, screen config and an optional scene.");
            painter.Bullet("Test - an isolated test module, wrapped in editor-only directives.");
            painter.Paragraph(
                "It writes the folder tree, Modules.<Name>.asmdef, and - unless you clear the "
                + "toggles - the Root and Context pair, and registers the module in the project's "
                + "module index the moment its folder exists. For a Screen module you can list the "
                + "screen's actions up front, and they are put on both the View and the Mediator.");

            painter.SubHeading("Create Command, Create Model, Create View");
            painter.Paragraph(
                "The same idea at a smaller scale. Each asks which module and which sub-module the "
                + "class belongs to, then writes it into the right folder with the right namespace.");
            painter.Note(
                "Create View also builds the prefab and adds the ViewInjector component. That "
                + "component is easy to forget by hand, and a View without it silently never "
                + "registers - no error, just a screen that does nothing.");

            painter.SubHeading("Delete Module");
            painter.Paragraph(
                "Removes the folder, its assembly definition and its metadata together. Deleting a "
                + "module by hand leaves its asmdef reference behind in other modules, which fails "
                + "to compile in a way that never names the module you deleted.");
        }
    }
}

#endif