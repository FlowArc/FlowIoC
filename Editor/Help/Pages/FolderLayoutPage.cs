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
            new HelpTreeNode("Scriptables", "",
                new HelpTreeNode("ScreenConfigs", "one config per screen or popup")),
            new HelpTreeNode("Scripts", "",
                new HelpTreeNode("Editor", "editor-only tooling for this module"),
                new HelpTreeNode("Runtime", "",
                    new HelpTreeNode("Constants", "values that never change"),
                    new HelpTreeNode("Controllers", "commands - one unit of work each"),
                    new HelpTreeNode("Datas", "",
                        new HelpTreeNode("UnityObjects", "ScriptableObject assets - CD_, RD_, PD_, ED_, DD_"),
                        new HelpTreeNode("ValueObjects", "plain data - VO, CVO, RVO, PVO, EVO, DVO")),
                    new HelpTreeNode("Entities", "runtime objects the module owns"),
                    new HelpTreeNode("Enums", "enumerations this module defines"),
                    new HelpTreeNode("Functions", "return a value, orchestrate nothing"),
                    new HelpTreeNode("Models", "state and the rules that keep it valid"),
                    new HelpTreeNode("RootsContexts", "the Root in the scene and the Context that binds"),
                    new HelpTreeNode("Services", "self-contained work, reusable in any game"),
                    new HelpTreeNode("Signals", "the module's whole public surface"),
                    new HelpTreeNode("Systems", "specific to this game, may lean on other systems"),
                    new HelpTreeNode("ViewsMediators", "scene references, and the mediator that drives them")),
                new HelpTreeNode("Shared", "optional - an assembly of its own, holding the data this module publishes",
                    new HelpTreeNode("Constants", "constants the shared data needs"),
                    new HelpTreeNode("Datas", "",
                        new HelpTreeNode("UnityObjects", "shared ScriptableObject assets"),
                        new HelpTreeNode("ValueObjects", "shared plain data")),
                    new HelpTreeNode("Enums", "enumerations the shared data needs"))),
            new HelpTreeNode("zScreenModules", "screens of this module - each one a module of its own"),
            new HelpTreeNode("zSubModules", "sub modules, which may use their parent's types"),
            new HelpTreeNode("zTestModules", "test code, wrapped in #if UNITY_EDITOR, may reference anything"));

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A module is a folder with an assembly definition of its own. That assembly is what "
                + "makes the boundary real: a module cannot accidentally reach into another one, "
                + "because the reference is simply not there.");
            painter.Paragraph(
                "Do not create the folders by hand. Tools > FlowIoC > Create Module writes them, and "
                + "both the code generators and the namespace tools depend on the exact shape it produces.");

            painter.Space();
            painter.Tree(Root);

            painter.Space();
            painter.Note(
                "Create Command, Create Model and Create View place their files in the right folder "
                + "on their own. Prefer them over writing files by hand.");

            painter.SubHeading("The direction of a sub module");
            painter.Paragraph(
                "A screen or a sub module may use its parent's types. The direction is one way: a "
                + "module never knows what sits in its own zScreenModules or zSubModules.");

            painter.SubHeading("Publishing data through Shared");
            painter.Paragraph(
                "Scripts/Shared is an assembly of its own - Modules.Player.Shared, beside "
                + "Modules.Player - and it is how a module hands data to another module without "
                + "handing over its logic. Only data belongs there: value objects, the "
                + "ScriptableObjects built out of them, and the enums and constants those need.");
            painter.Paragraph(
                "Whoever reads that data references Modules.Player.Shared, never Modules.Player. A "
                + "PlayerScreenModule can read CD_PlayerRules and still has no way to reach "
                + "PlayerModel or AddCurrencyCommand. Tick Shared when creating a main module and "
                + "Create Module writes the reference for you - into the module's own assembly, and "
                + "into every screen, sub and test module created under it afterwards.");
            painter.Note(
                "Shared is offered on main modules only. If two modules need the same data and "
                + "neither owns it, that data belongs in a module of its own - the same answer as "
                + "for a Service more than one module needs.");
        }
    }
}

#endif