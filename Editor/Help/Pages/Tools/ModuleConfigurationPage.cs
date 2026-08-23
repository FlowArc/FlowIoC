#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ModuleConfigurationPage : HelpPage
    {
        public ModuleConfigurationPage() : base(null)
        {
        }

        public override string Title => "Module Configuration";

        public override string Icon => "Settings";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Every module carries a _module_info.txt describing where it lives and what "
                + "namespace it uses. Moving folders in the Project window leaves that description "
                + "behind, and the generators then write into the wrong place. These entries put it "
                + "back in step.");

            painter.SubHeading("Tools > FlowIoC > Module Configuration");
            painter.Bullet("Detect & Fix Module Infos - finds modules whose metadata is missing, stale or pointing at a renamed folder, and repairs them.");
            painter.Bullet("Cleanse Module Infos - removes orphaned metadata for modules that no longer exist.");
            painter.Bullet("Update Namespace Settings - changes the namespace prefix the generators use.");
            painter.Note(
                "Run Detect & Fix after moving folders around. The symptom that you needed it is a "
                + "generator writing into the wrong place, or a Root whose sub-context list has "
                + "gone empty.");

            painter.SubHeading("Update Module's Namespaces");
            painter.Paragraph(
                "Assets > FlowIoC > Update Module's Namespaces. Select a module folder and every "
                + "namespace inside is rewritten to match where the module now sits. This is the "
                + "counterpart to renaming a module: Unity moves the files, this fixes the code.");

            painter.SubHeading("Assembly Creator");
            painter.Paragraph(
                "Assets > FlowIoC > Create Assembly adds an assembly definition for the selected "
                + "folder, named to the module convention; the Assembly Creator Window does the "
                + "same for several modules at once.");
            painter.Paragraph(
                "Assembly definitions are what make a module's boundary real. Without one, \"this "
                + "module does not reference that module\" is a rule nobody enforces.");
        }
    }
}

#endif
