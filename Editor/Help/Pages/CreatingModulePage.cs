#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The first thing a reader does in a FlowIoC project, and the one step that has no
    /// by-hand equivalent: a module is what Create Module writes, not a folder that happens
    /// to look like one. What each field of the panel decides is the tool page under Editor
    /// Tools; this is why the panel exists and what the three module types are for.
    /// </summary>
    internal class CreatingModulePage : HelpPage
    {
        public CreatingModulePage() : base(null)
        {
        }

        public override string Title => "Creating a Module";

        public override string Subtitle => "Start every module here";

        public override FlowIcon Icon => FlowIcon.Plus;

        protected override string BodyHeadline => "Every module is written by Create Module.";

        protected override string BodyTagline =>
            "Everything in a FlowIoC project lives in a module, and the panel is not a convenience "
            + "over making the folders yourself - it is the only supported way to add one.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("Why the panel and not a folder");
            painter.Paragraph(
                "A module is a folder tree, an assembly definition, a namespace and an entry in the "
                + "project's module index, and the four have to agree. The panel writes all of them "
                + "in one step from the name you type.");
            painter.Bullet(
                "Copying an existing module carries the original's namespace and assembly name with "
                + "it. Unity then refuses the duplicate assembly name without saying which of the "
                + "two copies is at fault.");
            painter.Bullet(
                "The index is rebuilt from the folder names on every Editor session, so a folder "
                + "you name yourself does turn up in it - as a module with no assembly definition "
                + "and none of the folders the other generators write into. Create Command then "
                + "offers it as a destination and has nowhere to put the file.");
            painter.Bullet(
                "The folder names themselves come from the directory config the panel shows beside "
                + "the module type. Renaming that config renames the folders the next module gets; "
                + "renaming a folder by hand only breaks the agreement.");

            painter.Space();
            painter.PageLink("Create Module", "The panel, field by field");

            painter.Separator();
            painter.SubHeading("The three module types");
            painter.Paragraph(
                "The Module Type dropdown picks which directory config is used, what the module is "
                + "named, and where in the tree it is allowed to sit.");

            painter.Rule("Main - the ordinary feature module.");
            painter.Paragraph(
                "Named <Name>Module and written under Assets/Modules/. This is what a Player, a Map "
                + "or an Inventory is. A Main module is the only kind that hosts the others, so it "
                + "cannot be nested inside a Screen or a Test module.");

            painter.Rule("Screen - one UI screen or popup.");
            painter.Paragraph(
                "Named <Name>ScreenModule and written into its parent's zScreenModules folder. It "
                + "comes with a View, a Mediator, its signal holder, and a context deriving from "
                + "ScreenSubContext that declares where the prefab lives - added to the parent "
                + "module's Root for you - and optionally its own scene. A screen belongs to the "
                + "module whose feature it shows, so its parent is a main or a sub module: another "
                + "screen module and a test module are not offered as one.");

            painter.Rule("Test - editor-only code that may reach anywhere.");
            painter.Paragraph(
                "Named <Name>TestModule and written into its parent's zTestModules folder, with "
                + "every script wrapped in UNITY_EDITOR directives so none of it ships. A test "
                + "module is the one place allowed to reference any module in the project, which is "
                + "what makes it useful and what keeps it out of a build. It attaches to the module "
                + "it tests, so it cannot be nested inside another test module.");

            painter.Note(
                "Delete a module through its own panel for the same reason. Deleting the folder by "
                + "hand leaves its asmdef reference behind in every module that named it, and the "
                + "compile error that follows never mentions the module you removed.");
            painter.PageLink("Delete Module", "Read: Delete Module");
            painter.PageLink("Rename Module", "Read: Rename Module");
        }
    }
}

#endif