#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ModuleScannerPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public ModuleScannerPage() : base(null)
        {
        }

        public override string Title => "Module Scanner";

        public override FlowIcon Icon => FlowIcon.Search;

        protected override string BodyHeadline => "Every module in the project, and what each one is missing.";

        protected override string BodyTagline =>
            "Tools > FlowIoC > Module Scanner finds them by walking the folder tree rather than by "
            + "trusting the index, so it is right even when the index is not.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("ModuleScannerWindow.png"),
                "Tools > FlowIoC > Module Scanner. Each module sits under the module it lives in, "
                + "and the one red row is a card whose purpose nobody has written yet.");

            painter.SubHeading("What it checks");
            painter.Table(new[] {"Check", "What it looks at"},
                new[] {"Mandatory folders", "The folders this module type's layout says must exist."},
                new[]
                {
                    "Shared assembly",
                    "A module with a Scripts/Shared folder must have the assembly that folder is for, "
                    + "or the data it means to publish stays inside its own."
                },
                new[]
                {
                    "Signals assembly",
                    "A module whose Scripts/Signals folder holds a signal holder must have the "
                    + "assembly that folder is for, or nothing outside the module can reach its public "
                    + "surface. An empty folder is left alone: a Connector announces nothing, so it "
                    + "owes no assembly."
                },
                new[] {"Assembly definition", "One asmdef at the module root, named to the module convention."},
                new[]
                {
                    "References",
                    "Its own Shared and Signals assemblies, the Shared assembly of the module it lives "
                    + "in, and for a test module that module's Signals and its own assembly."
                },
                new[]
                {
                    "Signals-to-Shared reference",
                    "A module's Signals assembly names the module's own Shared assembly, because a "
                    + "public signal is often generic over a type the module publishes and the two are "
                    + "separate assemblies. Without it the holder does not compile and the compiler "
                    + "reports CS0012."
                },
                new[]
                {
                    "Signal references",
                    "No module but a Connector names another module's .Signals assembly. Reading a "
                    + "neighbour's Shared data is ordinary; reaching its signals is the crossing a "
                    + "Connector exists for. The row reports and does not repair: what to do instead "
                    + "is a decision about the game."
                },
                new[]
                {
                    "Root and Context",
                    "A Root and the Context it roots carry the same name in front of the suffix - "
                    + "CounterServiceRoot roots CounterServiceContext, PlayerSystemRoot roots "
                    + "PlayerSystemContext - read off the Root's own declaration. A Root renamed for "
                    + "its colour with the Context left behind is reported and not repaired: the "
                    + "rename reaches everything that names the Context."
                },
                new[]
                {
                    "Namespace settings",
                    "The .csproj.DotSettings at the project root that tells Rider which folders "
                    + "produce a namespace."
                },
                new[]
                {
                    "Log channels",
                    "A log names no channel, because the module is read off the file the call sits "
                    + "in. A FlowLogger call whose first argument is a string typed by hand, "
                    + "FlowModule.Default, or the constant of the very module it sits in is reported "
                    + "with its file and line. Fix cuts the channel where only the message follows, "
                    + "and writes the module's constant where a profile or a context follows; a "
                    + "literal that is exactly another module's name becomes that module's constant. "
                    + "A Connector naming the module it wires is left alone."
                },
                new[]
                {
                    "Module card",
                    "Every module but a test module carries a MODULE.md, and its generated block "
                    + "still describes the module. A card whose purpose nobody has written is "
                    + "reported but not written for you: saying what a module is for is the one part "
                    + "of the card nothing can generate."
                },
                new[]
                {
                    "The project itself",
                    "The module index against the folder tree, the module directory and its ignore "
                    + "rule, orphaned settings files, the Flow Console log types, and the solution "
                    + "code style."
                },
                new[]
                {
                    "Project files",
                    "A .csproj left on a package folder the Package Manager has swept, or a solution "
                    + "listing a project file that is gone. Unity compiles from neither, so the "
                    + "project builds while the IDE reports every type in the package. Fix "
                    + "regenerates them all, the way Regenerate project files in Preferences does; "
                    + "FlowIoC also does that on its own the first time a version of it runs in a "
                    + "project, so the row is red only when that did not happen."
                });

            painter.SubHeading("Reading a row");
            painter.Paragraph(
                "A row wears the worst answer its checks gave: green for a module with nothing "
                + "wrong, amber for something Fix All repairs on its own, red for something only "
                + "a person can. The whole row is the foldout, so clicking anywhere on it shows "
                + "the findings behind the colour.");
            painter.Paragraph(
                "A module sits under the module it lives in, stepped in and hung from its parent's "
                + "row by a guide line - sub modules first, then screens, then tests. The foldout "
                + "opens a module's own findings and nothing else: what is inside it is always "
                + "listed, because which module sits in which is what the tree is there to show. "
                + "\"Only issues\" hides every row that is already green, except a parent whose "
                + "child has something to say - it stays, so the child still has a row to hang "
                + "from.");
            painter.Paragraph(
                "The word at a row's right edge is what the module's Root roots - SYSTEM, SERVICE, "
                + "CONNECTOR or CORE - in the colour that Root wears in the inspector, read off the "
                + "Root the same way its own header bar reads it. A screen module says SCREEN and a "
                + "test module TEST: for those two the kind is the role. The same badge sits on "
                + "every module list FlowIoC draws.");

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
                "Important: Assets/Plugins/FlowIoC/MODULES.md is generated and gitignored - it is "
                + "a cache the next compile writes again, and two branches adding modules would "
                + "otherwise conflict over the same list. The rule sits in a .gitignore beside it, "
                + "inside FlowIoC's own folder; your project's root .gitignore is never touched.");

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