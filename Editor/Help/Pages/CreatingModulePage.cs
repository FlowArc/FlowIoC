#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The first thing a reader does in a FlowIoC project, and the one step that has no
    /// by-hand equivalent: a module is what Create Module writes, not a folder that happens
    /// to look like one.
    /// </summary>
    internal class CreatingModulePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreatingModulePage() : base(null)
        {
        }

        public override string Title => "Creating a Module";

        public override string Subtitle => "Start every module here";

        public override string Icon => "CreateAddNew";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Everything in a FlowIoC project lives in a module, and every module is written by "
                + "Tools > FlowIoC > Create Module. The panel is not a convenience over making the "
                + "folders yourself - it is the only supported way to add one.");

            painter.Image(_images.Get("CreateModuleWindow.png"),
                "Tools > FlowIoC > Create Module, filled in for a Main module named Player.");

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
                + "module's Root for you - and optionally its own scene. List the screen's actions in "
                + "the panel and they are put on both the View and the Mediator, so the button you "
                + "name here arrives already wired. A screen belongs to the module whose feature it "
                + "shows, which is the direction the nesting rule enforces.");

            painter.Rule("Test - editor-only code that may reach anywhere.");
            painter.Paragraph(
                "Named <Name>TestModule and written into its parent's zTestModules folder, with "
                + "every script wrapped in UNITY_EDITOR directives so none of it ships. A test "
                + "module is the one place allowed to reference any module in the project, which is "
                + "what makes it useful and what keeps it out of a build. It attaches to the module "
                + "it tests, so it cannot be nested inside another test module.");

            painter.SubHeading("What the toggles decide");
            painter.Bullet(
                "Create Root and Create Context write the pair that gives the module its presence "
                + "in the scene and its bindings. Leave both on unless you are adding a module that "
                + "another one launches.");
            painter.Bullet(
                "Role names the Root and the Context for what the Root roots, which is what the "
                + "inspector reads to colour it: System writes PlayerSystemRoot and "
                + "PlayerSystemContext, Service writes CounterServiceRoot and CounterServiceContext, "
                + "and Core writes the plain PlayerRoot and PlayerContext. It starts on System, "
                + "because a module written for the game at hand is one. The module folder, its "
                + "assembly and its namespaces are the same whichever you pick, and the dropdown is "
                + "offered on a main module that gets a Root.");
            painter.Bullet(
                "Allow As Sub Context writes AllowAsSubContext on the context. A context that has a "
                + "Root of its own is kept out of a Root's Add Sub Context list, because adding it "
                + "elsewhere would build it a second time; this puts it back, for a module meant to be "
                + "hosted on another module's Root. Offered on a main module that gets a Root, and off "
                + "by default.");
            painter.Bullet(
                "Create Signals writes the signal holder - the module's public surface. A module "
                + "with no signals can only be reached by referencing it directly, which is the "
                + "thing the architecture is there to prevent.");
            painter.Bullet(
                "Create Scene adds a scene of the module's own. Useful for a Screen module, "
                + "unnecessary for a module that only holds state.");
            painter.Bullet(
                "Create Shared adds the second assembly a module publishes its data through, so a "
                + "neighbour can read a config asset it authored without gaining access to its "
                + "Models and Commands. Offered on main modules and off by default: a module that "
                + "hands nothing to anyone has no use for it. Tools > FlowIoC > Add Shared Data "
                + "does the same to a module that already exists.");

            painter.SubHeading("Parent Module");
            painter.Paragraph(
                "The list at the bottom is where the new module is placed. Modules puts it at the "
                + "top level; picking an existing module nests it inside, under zSubModules, "
                + "zScreenModules or zTestModules depending on the type. A parent that cannot host "
                + "the type you chose is not offered. The Folder Structure Preview above shows "
                + "exactly what will be written before you commit to it, and the optional folders "
                + "are the ones you can tick off.");

            painter.Note(
                "Delete a module through Tools > FlowIoC > Delete Module for the same reason. "
                + "Deleting the folder by hand leaves its asmdef reference behind in every module "
                + "that named it, and the compile error that follows never mentions the module you "
                + "removed.");
        }
    }
}

#endif