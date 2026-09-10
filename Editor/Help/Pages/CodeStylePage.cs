#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The rules a settings file cannot express. Naming, prefixes, suffixes and spacing are
    /// declared in the solution's DotSettings and Module Scanner writes them, so what a page can
    /// usefully carry is only what a reader has to decide for themselves.
    /// </summary>
    internal class CodeStylePage : HelpPage
    {
        public CodeStylePage() : base(null)
        {
        }

        public override string Title => "Code Style";

        public override FlowIcon Icon => FlowIcon.Pencil;

        protected override string BodyHeadline => "Most of the style is a file. These four are not.";

        protected override string BodyTagline =>
            "Naming, prefixes, suffixes and spacing live in <Solution>.sln.DotSettings at the "
            + "project root, which Module Scanner writes. Read it before writing C#, and run the "
            + "scanner rather than editing it.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.PageLink("Module Scanner", "Read: Module Scanner");

            painter.Separator();
            painter.SubHeading("Methods that are alternatives share a verb prefix");
            painter.Paragraph(
                "Pressing . and typing the verb then offers the whole set, and a reader who knows "
                + "one of them finds the others without opening the documentation. The function "
                + "provider's three terminators are the worked example.");
            painter.Code(
                "// The set stays together under one verb.\n"
                + "Execute();\n"
                + "ExecuteAsync();\n"
                + "ExecuteAndGetResult<T>();\n"
                + "\n"
                + "// Each reads better alone, and the family is broken in half.\n"
                + "Run();\n"
                + "RunAsync();\n"
                + "GetResult<T>();");
            painter.Paragraph(
                "Weigh discoverability above the prettiest individual name whenever a caller has to "
                + "choose between the members of a set. It is the rule that groups a Service's "
                + "verbs under nouns, applied one level down: there it keeps the list short, here it "
                + "keeps siblings adjacent in it.");

            painter.Separator();
            painter.SubHeading("Keep static to what the engine forces");
            painter.Paragraph(
                "Static state cannot be reset between domain reloads, cannot be substituted in a "
                + "test, and hides the lifetime of whatever it caches. Write an ordinary class with "
                + "instance members and give it an owner that holds the instance.");
            painter.Paragraph(
                "Unity forces a few entry points - InitializeOnLoad, InitializeOnLoadMethod, "
                + "MenuItem, and the ScriptableObject and EditorWindow factory calls. Those stay as "
                + "thin as they can be: ideally a small bootstrap type whose only job is to hold the "
                + "one instance the callback needs.");

            painter.Separator();
            painter.SubHeading("Every enum value carries its number");
            painter.Paragraph(
                "Unity serializes an enum as an int. A value inserted in the middle, or a value "
                + "deleted, silently renumbers everything below it, and every asset already on disk "
                + "then reads back as the wrong thing.");
            painter.Code(
                "public enum FolderType\n"
                + "{\n"
                + "    Folder = 0,\n"
                + "    ViewsAndMediators = 1,\n"
                + "    Controllers = 2\n"
                + "}");
            painter.Paragraph(
                "Numbered, a value can be deleted outright and a new one takes the next free "
                + "number. A number a deleted value used is never reused.");

            painter.Separator();
            painter.SubHeading("A log message spells names out");
            painter.Paragraph(
                "Never nameof inside one. Written inside AddCurrencyCommand, "
                + "$\"{nameof(Execute)} - {nameof(AddCurrencyCommand)}\" is a real reference to the "
                + "type, so Find Usages and a plain search both answer where is this Command used "
                + "with the command's own logging lines.");
            painter.Code(
                "FlowLogger.Log(FlowLogType.PlayerModule, \"Execute - AddCurrencyCommand\");");
            painter.Paragraph(
                "The trade is that a rename leaves the literal stale, and a stale word in a log line "
                + "is cheaper than a search that cannot be trusted. This is about the message text "
                + "alone - nameof stays correct everywhere else.");
            painter.PageLink("Flow Console", "Read: Flow Console");

            painter.Note(
                "Important: the DotSettings files are written by Module Scanner, not by hand. An "
                + "edit made in Rider's own settings UI lands in the same files and is overwritten "
                + "the next time the scanner runs.");
        }
    }
}

#endif
