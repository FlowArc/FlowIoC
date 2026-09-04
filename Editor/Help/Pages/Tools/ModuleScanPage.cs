#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ModuleScanPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public ModuleScanPage() : base(null)
        {
        }

        public override string Title => "Module Scanner";

        public override string Icon => "Settings";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Module Scanner reads every module in the project and says what "
                + "each one is missing. A module is a folder whose name ends in \"Module\", under "
                + "Assets/Modules or inside an embedded package, and the panel finds them by "
                + "walking the folder tree rather than by trusting the index - so it is right "
                + "even when the index is not.");

            painter.Image(_images.Get("ModuleScannerWindow.png"),
                "Tools > FlowIoC > Module Scanner. One project finding is fixable, and every "
                + "module in this project is in order.");

            painter.SubHeading("What it checks");
            painter.Bullet(
                "Mandatory folders - the folders this module type's layout says must exist.");
            painter.Bullet(
                "Shared assembly - a module with a Scripts/Shared folder must have the assembly "
                + "that folder is for, or the data it means to publish stays inside its own.");
            painter.Bullet(
                "Assembly definition - one asmdef at the module root, named to the module "
                + "convention.");
            painter.Bullet(
                "References - its own Shared assembly, the Shared assembly of the module it lives "
                + "in, and for a test module that module's own assembly.");
            painter.Bullet(
                "Namespace settings - the .csproj.DotSettings at the project root that tells "
                + "Rider which folders produce a namespace.");
            painter.Bullet(
                "The project itself - the module index against the folder tree, orphaned settings "
                + "files, the Flow Console log types, and the solution code style.");

            painter.SubHeading("Reading a row");
            painter.Paragraph(
                "A row wears the worst answer its checks gave: green for a module with nothing "
                + "wrong, amber for something Fix All repairs on its own, red for something only "
                + "a person can. The whole row is the foldout, so clicking anywhere on it shows "
                + "the findings behind the colour, and \"Only issues\" hides every row that is "
                + "already green.");

            painter.SubHeading("Fix All");
            painter.Paragraph(
                "One button repairs everything that can be repaired without guessing. It creates "
                + "folders, writes a missing asmdef from the same template Create Module uses, "
                + "adds missing references without touching ones you added by hand, writes the "
                + "namespace settings, rebuilds the index and sweeps orphaned files.");
            painter.Paragraph(
                "What it will not do is rename an assembly or remove a reference. Renaming one "
                + "moves every asmdef that names it and the settings file named after it, which "
                + "is more than a scan should decide. Those rows stay red and say what to do.");

            painter.Note(
                "Important: a module with no assembly definition is invisible to the namespace "
                + "settings. The writer skips it silently, so its scripts keep whatever namespace "
                + "they were written with and nothing reports it. Module Scanner is where that gap "
                + "becomes visible, and Fix All is what closes it.");

            painter.SubHeading("On editor load");
            painter.Paragraph(
                "The module index and the log types are rebuilt on every editor load, as they "
                + "always were. If anything else is wrong the console carries one line saying how "
                + "many issues there are and where to look. A clean project says nothing.");
        }
    }
}

#endif