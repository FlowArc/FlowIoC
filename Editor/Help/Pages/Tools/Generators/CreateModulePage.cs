#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The panel itself: what each field and toggle decides, and where the module lands. Why a
    /// module is generated at all rather than made by hand is the Wiki's Creating a Module, and
    /// this page is what a reader opens with the window in front of them.
    /// </summary>
    internal class CreateModulePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateModulePage() : base(null)
        {
        }

        public override string Title => "Create Module";

        public override string Icon => "CreateAddNew";

        protected override string BodyHeadline => "Tools > FlowIoC > Create Module.";

        protected override string BodyTagline =>
            "The folder tree, the assembly definition, the namespace settings, the Root and Context "
            + "pair and the entry in the module index, written in one step from the name you type.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateModuleWindow.png"),
                "Create Module, filled in for a Main module named Player.");

            painter.Paragraph(
                "Why a module is never a folder you make yourself is on the Wiki page this one sits "
                + "under. What follows is the panel.");
            painter.PageLink("Creating a Module", "Read: Creating a Module");

            painter.Separator();
            painter.SubHeading("Name, and what the generator adds to it");
            painter.Paragraph(
                "Type the thing's own name and nothing else. Module Type decides the suffix: Player "
                + "becomes PlayerModule, and on a Screen module Settings becomes SettingsScreenModule. "
                + "Typing SettingsScreen produces SettingsScreenScreenModule, and the folder, the "
                + "assembly and every namespace under it carry the stutter.");

            painter.Separator();
            painter.SubHeading("What the toggles decide");
            painter.Bullet(
                "Create Root and Create Context write the pair that gives the module its presence in "
                + "the scene and its bindings. Leave both on unless the module is one another Root "
                + "hosts as a sub-context.");
            painter.Bullet(
                "Role names the pair for what the Root roots, which is what the inspector reads to "
                + "colour it. System writes PlayerSystemRoot and PlayerSystemContext, Service writes "
                + "CounterServiceRoot and CounterServiceContext, and Core writes the plain PlayerRoot "
                + "and PlayerContext with FlowHeader(FlowRole.Core) above the Root, a Core module "
                + "carrying no suffix for the colour to be read from. It starts on System, because a "
                + "module written for the game at hand is one.");
            painter.Bullet(
                "Allow As Sub Context writes AllowAsSubContext on the context. A context with a Root "
                + "of its own is kept out of a Root's Add Sub Context list, since adding it elsewhere "
                + "would build it a second time; this puts it back. Off by default.");
            painter.Bullet(
                "Create Public Signals writes the public holder into Scripts/Signals and binds it "
                + "in the Context. On by default, forced on for a Screen module - which generates no "
                + "Context of its own, so the holder is the only way in - and not offered for a Test "
                + "module, whose holder stays inside its own assembly. Untick it for a module that "
                + "announces nothing: a Service that answers the caller it was given, or a Connector, "
                + "which wires other modules' signals and owns none.");
            painter.Bullet(
                "\"Public\" is in the label because a module has two signal holders and only this "
                + "one crosses a boundary. The other is written whether or not anything is ticked, "
                + "and the two folders are both called Signals on purpose: the namespace segment is "
                + "the folder name, so sharing it is what lets one using reach both holders. The "
                + "Folder Structure Preview says which row is which.");
            painter.Bullet(
                "Create Shared adds the assembly a module publishes its data through. Off by "
                + "default: a module that hands no data to anyone has no use for it.");
            painter.Bullet(
                "Create Scene adds a scene of the module's own, so the module can be opened and "
                + "played on its own.");
            painter.Bullet(
                "The optional folders are the rest - Resources, Editor, Scenes, Prefabs. The Folder "
                + "Structure Preview beside them shows exactly what will be written.");

            painter.Note(
                "Important: Create Public Signals and Create Shared are offered on the day the module is "
                + "created and never again. A module that turns out to need either later is not "
                + "stuck - Add Shared or Signals writes the same thing into a module that already "
                + "exists.");
            painter.PageLink("Add Shared or Signals", "Read: Add Shared or Signals");

            painter.Separator();
            painter.SubHeading("Parent Module");
            painter.Paragraph(
                "The list at the bottom is where the module is placed: the project's module tree, "
                + "a module under the module it lives in, and clicking a row picks it. Modules, the "
                + "row the whole tree hangs from, puts it at the top level; picking an existing "
                + "module nests it inside, under zSubModules, zScreenModules or zTestModules "
                + "according to the type. A parent that cannot host the type you chose is drawn dim "
                + "and takes no click - a screen belongs to the module whose feature it shows, so "
                + "another screen module and a test module are not parents.");

            painter.Separator();
            painter.SubHeading("What a Screen module gets on top");
            painter.Paragraph(
                "A View, a Mediator, a context deriving from ScreenSubContext that declares where "
                + "the prefab lives, and the actions you list in the panel - each written onto both "
                + "the View and the Mediator, so the button you name here arrives already wired.");
            painter.Paragraph(
                "The screen's context is added to the parent module's Root as a sub-context entry. "
                + "With Create Scene ticked the generator also builds the prefab with a ViewInjector "
                + "on it, parents it under the ScreenManager's first layer, adds an EventSystem and "
                + "a test Root that hosts the screen's real context, and registers the prefab in "
                + "Addressables.");

            painter.Note(
                "Important: the Add Action button adds a row to the action list. It is not the "
                + "button that writes the module - that is Create Module, pinned under the form at "
                + "the bottom of the window.");

            painter.Note(
                "Important: only the files the generator writes are wrapped in UNITY_EDITOR "
                + "directives on a Test module. Anything you add to it afterwards needs the same "
                + "guard, or editor-only code reaches a build.");
        }
    }
}

#endif