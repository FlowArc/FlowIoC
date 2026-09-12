#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The generator that changes a name rather than adding or removing a module. What makes it
    /// worth a page of its own is how far a name reaches, and the line the panel draws between what
    /// was written from the name and what somebody named.
    /// </summary>
    internal class RenameModulePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public RenameModulePage() : base(null)
        {
        }

        public override string Title => "Rename Module";

        public override FlowIcon Icon => FlowIcon.Pencil;

        protected override string BodyHeadline => "Tools > FlowIoC > Rename Module.";

        protected override string BodyTagline =>
            "A module's name reaches its assemblies, its namespaces, its settings files, its log channel, "
            + "the classes written from it and the Roots that list its contexts. The panel carries a new "
            + "name to all of them in one press.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("RenameModuleWindow.png"),
                "The module tree, the name, and everything the press will do - listed before it is pressed.");

            painter.SubHeading("Pick, type, read, press");
            painter.Paragraph(
                "The list is the project's module tree, a module under the module it lives in. Click a "
                + "row to pick it and type the new name without its suffix - the panel adds Module, "
                + "TestModule or ScreenModule after the field, the way Create Module does. Under the name "
                + "is what the press will do, rebuilt as you type: the folder, the assemblies, the settings "
                + "files, the namespaces, the classes, the assets, the Flow Console channel - old name to "
                + "new name, one line each - and a reason on every line that stays as it is.");
            painter.Paragraph(
                "MainModule, ConnectorModule and ScreenModule cannot be picked, for the reasons Delete "
                + "Module gives on the same rows: the project is built on them by name.");

            painter.Separator();
            painter.SubHeading("The line it draws");
            painter.Paragraph(
                "The panel renames what was written from the module's name, and nothing you named. "
                + "Create Module wrote the Root, the Context and the two signal holders from the name, "
                + "and for a screen the View, the Mediator and the prefab, so those follow - found on "
                + "disk by their names, with every use across the project rewritten. A Model, a Command "
                + "or a View you added afterwards keeps its name even when it starts with the old one, "
                + "and so does the card's own text.");
            painter.Bullet("The folder, and the folders of the modules inside it that carry its name.");
            painter.Bullet(
                "Its assembly definitions - its own, its Shared and its Signals - and the entry in every "
                + "asmdef in the project that references them.");
            painter.Bullet("Every namespace under Modules.<Name>, in every source file in the project.");
            painter.Bullet(
                "The settings files at the project root: renamed after the new assemblies, and rewritten "
                + "for every module inside the folder, because the paths inside them changed.");
            painter.Bullet(
                "The Flow Console channel, keeping its number and colour, the generated FlowLogType part, "
                + "and every FlowLogType.<Name> in code.");
            painter.Bullet(
                "The prefabs and scenes in the module's own Prefabs, Scenes and Resources folders whose "
                + "name starts with the old one - the test scene, a Root prefab named after its class, "
                + "the screen prefab. Renaming keeps the asset's GUID, so Build Settings and prefab "
                + "instances do not notice.");
            painter.Bullet(
                "A screen's Addressables address and group, when the address is still the one the "
                + "generator gave it, and the address literal in the screen's context.");
            painter.Bullet("The module index, and the card's heading. The card's generated block is rewritten after the compile.");

            painter.Separator();
            painter.SubHeading("Modules inside it");
            painter.Paragraph(
                "A module inside the one you rename follows when its name starts with the parent's: "
                + "CounterTestModule follows CounterModule, GameplayScreenModule follows GameplayModule, "
                + "and GameplayScreenTestModule then follows the screen. Each is listed with a tick. "
                + "Untick one and it keeps its name, and so does everything inside it - a child's new "
                + "name is built on its parent's. A nested module that does not carry the name keeps its "
                + "folder; its namespace and its settings file still change, because they name the parent.");

            painter.Separator();
            painter.SubHeading("The Roots that list its contexts");
            painter.Paragraph(
                "A Root lists a sub-context by the context's full name, and that is what runtime resolves. "
                + "So the panel rewrites the entry in every scene and prefab that lists one of the module's "
                + "contexts - the module's own test scene included. A prefab is written. A scene that is "
                + "not open is opened, written and closed. A scene that is open is changed and left dirty, "
                + "because whatever else is unsaved in it is yours: save it to keep the change, or close "
                + "it without saving and open the Root once - the inspector repairs the entry from the "
                + "script it references.");

            painter.Note(
                "Important: a class whose name is also a class somewhere else in the project is skipped, "
                + "and the preview says so under Warnings. The rename is a text pass over every source "
                + "file, and it would reach the other class too. Rename that one in your IDE.");

            painter.Note(
                "Important: a ready-made module - one the package installed from its own set - is "
                + "recognised by its assembly name. Renamed, it counts as the game's own, and the "
                + "installer will offer the original again. That is a second copy, not an update.");

            painter.Note(
                "Important: this cannot be undone as an Undo step, but nothing is lost. Every GUID is "
                + "kept and every change is a name, so renaming the module back through this panel "
                + "restores it.");

            painter.PageLink("Delete Module", "Read: Delete Module");
            painter.PageLink("Module Scanner", "Read: Module Scanner");
        }
    }
}

#endif
