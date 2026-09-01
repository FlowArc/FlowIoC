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
                "Every module is recorded in one project asset, ED_ModuleIndex.asset, keyed "
                + "on its folder's own Unity GUID rather than its name or path. A rename or a move "
                + "made outside the Editor can still leave that index stale, and the generators "
                + "then write into the wrong place. These entries put it back in step.");

            painter.SubHeading("Tools > FlowIoC > Module Configuration");
            painter.Bullet(
                "Detect & Fix Module Index - rescans the folder tree and rebuilds the module index, so every module's name, kind and location match what is actually on disk.");
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