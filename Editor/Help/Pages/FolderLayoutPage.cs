#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages
{
    internal class FolderLayoutPage : HelpPage
    {
        public FolderLayoutPage() : base(null)
        {
        }

        public override string Title => "Folder Layout";

        public override string Icon => "Folder Icon";

        /// <summary>
        /// Exposed so a test can check the tree against the folder list Create Module actually
        /// writes. The window itself only ever draws it.
        /// </summary>
        public HelpTreeNode Root { get; } = new HelpTreeNode("PlayerModule", "one module, one assembly",
            new HelpTreeNode("Modules.Player.asmdef", "its own assembly - this is what stops a stray reference"),
            new HelpTreeNode("Prefabs", "prefabs this module owns"),
            new HelpTreeNode("Resources", "assets loaded by name at runtime"),
            new HelpTreeNode("Scenes", "scenes this module owns"),
            new HelpTreeNode("Scriptables", "ScriptableObject assets the module authors by hand"),
            new HelpTreeNode("Scripts", "",
                new HelpTreeNode("Editor", "editor-only tooling for this module"),
                new HelpTreeNode("Runtime", "",
                    new HelpTreeNode("Constants", "values that never change"),
                    new HelpTreeNode("Controllers", "commands and functions - the module's work"),
                    new HelpTreeNode("Data", "",
                        new HelpTreeNode("UnityObjects", "ScriptableObject assets - CD_, RD_, PD_, ED_, DD_"),
                        new HelpTreeNode("ValueObjects", "plain data - VO, CVO, RVO, PVO, EVO, DVO")),
                    new HelpTreeNode("Entities", "runtime objects the module owns"),
                    new HelpTreeNode("Enums", "enumerations this module defines"),
                    new HelpTreeNode("Models", "state and the rules that keep it valid"),
                    new HelpTreeNode("RootsContexts", "the Root in the scene and the Context that binds"),
                    new HelpTreeNode("Services", "self-contained work, reusable in any game"),
                    new HelpTreeNode("Signals", "PlayerInternalSignals - what the module says to itself"),
                    new HelpTreeNode("Systems", "specific to this game, may lean on other systems"),
                    new HelpTreeNode("ViewsMediators", "scene references, and the mediator that drives them")),
                new HelpTreeNode("Shared", "Modules.Player.Shared - the data this module publishes",
                    new HelpTreeNode("Constants", "constants the shared data needs"),
                    new HelpTreeNode("Data", "",
                        new HelpTreeNode("UnityObjects", "shared ScriptableObject assets"),
                        new HelpTreeNode("ValueObjects", "shared plain data")),
                    new HelpTreeNode("Enums", "enumerations the shared data needs")),
                new HelpTreeNode("Signals", "Modules.Player.Signals - PlayerSignals, the module's whole public surface")),
            new HelpTreeNode("zScreenModules", "screens of this module - each one a module of its own"),
            new HelpTreeNode("zSubModules", "sub modules, which may use their parent's types"),
            new HelpTreeNode("zTestModules", "test code, wrapped in #if UNITY_EDITOR, may reference anything"));

        protected override string BodyHeadline => "A module is a folder with an assembly definition of its own.";

        protected override string BodyTagline =>
            "That assembly is what makes the boundary real: a module cannot accidentally reach into "
            + "another one, because the reference is simply not there.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Do not create the folders by hand. Tools > FlowIoC > Create Module writes them, and "
                + "both the code generators and the namespace tools depend on the exact shape it produces.");

            painter.Space();
            painter.Tree(Root);

            painter.Space();
            painter.Note(
                "Create Command, Create Function, Create Model and Create View place their files in the right "
                + "folder on their own. Prefer them over writing files by hand.");

            painter.SubHeading("The direction of a sub module");
            painter.Paragraph(
                "A screen or a sub module may use its parent's types. The direction is one way: a "
                + "module never knows what sits in its own zScreenModules or zSubModules.");

            painter.Separator();
            painter.SubHeading("What a module carries with it");
            painter.Paragraph(
                "Everything a module offers arrives with the module. A test module brings the scene "
                + "it runs in, already built, rather than a menu item that builds it, so installing "
                + "the module is the only step there is. Nothing a module owns belongs on the "
                + "Tools > FlowIoC menu, which stays the framework's own.");

            painter.Separator();
            painter.SubHeading("Three assemblies, and who may reference each");
            painter.Paragraph(
                "Scripts/Runtime is Modules.Player - the Models, Commands, Views and the internal "
                + "signal holder, which nothing outside the module ever sees. Scripts/Shared is "
                + "Modules.Player.Shared - the data this module publishes, and the enums and "
                + "constants that data needs. Scripts/Signals is Modules.Player.Signals - "
                + "PlayerSignals, and nothing else.");
            painter.Paragraph(
                "The split between the last two is what makes the architecture hold by itself. A "
                + "System legitimately references a neighbour's Shared assembly to read a published "
                + "enum, and while the holder lived in Shared that same reference put the "
                + "neighbour's signals in scope - a cross-module Dispatch compiled, and only "
                + "discipline stopped it. Now it does not compile, and signals cross through a "
                + "Connector because the compiler says so.");
            painter.Paragraph(
                "Whoever reads that data references Modules.Player.Shared, never Modules.Player. A "
                + "PlayerScreenModule can read CD_PlayerRules and still has no way to reach "
                + "PlayerModel, AddCurrencyCommand or PlayerSignals. Tick Shared when creating a "
                + "main module and Create Module writes the reference for you - into the module's "
                + "own assembly, and into every screen, sub and test module created under it "
                + "afterwards.");
            painter.Paragraph(
                "Namespaces follow the folder here too: a value object under "
                + "Scripts/Shared/Data/ValueObjects is in "
                + "Modules.PlayerModule.Shared.Data.ValueObjects, so it cannot collide with the "
                + "Runtime type of the same name. The public holder under Scripts/Signals lands in "
                + "Modules.PlayerModule.Signals - the namespace the internal holder is already in, "
                + "so one using reaches both. The generator writes a .csproj.DotSettings per "
                + "assembly for that, beside the module's own.");
            painter.Note(
                "Both are ticks in Create Module. Signals starts ticked, because most modules have "
                + "a public surface; Shared starts unticked, because a module pays for that "
                + "assembly on the day it publishes something. A test module is offered neither, "
                + "and a screen module cannot decline Signals - it generates no Context of its own, "
                + "so the holder is the only way in. Everywhere else the Signals tick comes off "
                + "freely: a Service that answers its caller rather than announcing, and a "
                + "Connector that owns no signals at all, are finished without the folder, and "
                + "nothing puts it back. If two modules need the same data and neither owns it, "
                + "that data belongs in a module of its own - the same answer as for a Service more "
                + "than one module needs.");
        }
    }
}

#endif