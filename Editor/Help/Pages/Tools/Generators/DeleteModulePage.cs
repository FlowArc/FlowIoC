#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The one generator that takes something away, and the only one with a question of its own to
    /// ask. Its three-answer dialog, and what each answer does to a prefab, a closed scene and an
    /// open one, is the whole reason this panel has a page rather than a sub-heading.
    /// </summary>
    internal class DeleteModulePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public DeleteModulePage() : base(null)
        {
        }

        public override string Title => "Delete Module";

        public override string Icon => "TreeEditor.Trash";

        protected override string BodyHeadline => "Tools > FlowIoC > Delete Module.";

        protected override string BodyTagline =>
            "A module is more than its folder, so removing one is more than deleting it. The panel "
            + "unwires the module from everywhere that names it, and then deletes.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("DeleteModuleWindow.png"),
                "Every module in the project, searchable, with a Delete beside each.");

            painter.SubHeading("What goes with the folder");
            painter.Bullet("The module folder and everything under it, sub-modules included.");
            painter.Bullet(
                "Its assembly definitions - its own, its Shared and its Signals - read off the "
                + "asmdefs inside the folder rather than derived from the module's name, so a "
                + "module that holds sub-modules takes their assemblies with it.");
            painter.Bullet("The project files and namespace settings named after those assemblies.");
            painter.Bullet("Its entry in the module index, and its Flow Console log channel.");
            painter.Bullet(
                "Its assemblies stripped out of every asmdef in the project that named them. Deleting "
                + "a module by hand leaves those references behind, and the compile error that "
                + "follows names the module that referenced it rather than the one you removed.");
            painter.Bullet(
                "A screen module's Addressables entry, and the group it was in when that leaves the "
                + "group empty.");

            painter.Separator();
            painter.SubHeading("The question it asks first");
            painter.Paragraph(
                "A module's context can be listed as a sub-context on Roots outside it, and those "
                + "Roots are prefabs and scenes rather than code. Left alone they list a context "
                + "that will not exist, so the panel finds them and asks before anything is deleted. "
                + "There are three answers.");

            painter.Rule("Remove from all - take the entry out of every Root that lists it.");
            painter.Paragraph("No further questions. Each Root is written and named on the console.");

            painter.Space();
            painter.Rule("Ask me for each - one question per entry.");
            painter.Paragraph(
                "Both answers are honoured, and an entry you keep is reported as kept.");

            painter.Space();
            painter.Rule("Cancel, just report - see where they are and keep the module.");
            painter.Paragraph(
                "The list is written to the console and nothing else happens. It is the middle "
                + "button, so a stray Escape or a closed window lands on the answer that changes "
                + "nothing rather than on the one that edits every scene.");

            painter.Note(
                "Important: the question comes before anything is deleted, so cancelling leaves the "
                + "module whole - its assemblies, its settings files and its folder are all still "
                + "there.");

            painter.Separator();
            painter.SubHeading("How far it goes into a scene");
            painter.Paragraph(
                "It depends on what holds the Root. A prefab is a file and is written. A scene that "
                + "is not open is opened, written and closed again. A scene that is open is changed "
                + "and left dirty, because whatever else is unsaved in it belongs to whoever opened "
                + "it - so save it to keep the change, or close without saving to keep the entry. "
                + "Every entry removed, skipped or left is named on the console with its Root and "
                + "its asset.");

            painter.Note(
                "Important: a Connector's wiring is resolved at runtime. A Connector still pointing "
                + "at a deleted module's signals fails when the scene runs rather than when the "
                + "project compiles, so search for the module's signals before deleting it.");
            painter.PageLink("Connectors", "Read: Connectors");

            painter.Note(
                "Important: this cannot be undone. Deletion is not an Undo step - the folder and "
                + "the files are gone from disk, and the dialog says so before it starts.");
        }
    }
}

#endif
