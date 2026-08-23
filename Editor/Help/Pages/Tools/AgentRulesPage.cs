#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class AgentRulesPage : HelpPage
    {
        public AgentRulesPage() : base(null)
        {
        }

        public override string Title => "Agent Rules";

        public override string Icon => "console.infoicon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > AI > Agent Rules. FlowIoC imposes an architecture that nothing "
                + "in the C# type system enforces, so an AI coding assistant that has not been told "
                + "the rules will happily write code that compiles and breaks every one of them - "
                + "logic in a Context, one module injecting another's model, [Inject] on a field "
                + "where it is silently skipped.");
            painter.Paragraph(
                "The window writes those rules into your project's root AGENTS.md - the convention "
                + "Claude Code, Codex, Cursor, Zed and Gemini CLI all read - and points CLAUDE.md "
                + "at that file.");

            painter.SubHeading("Only inside the markers");
            painter.Code(
                "<!-- FLOWIOC:BEGIN version=<installed> hash=<rule text> | ... -->\n"
                + "...\n"
                + "<!-- FLOWIOC:END -->");
            painter.Paragraph(
                "Nothing outside those markers is ever touched, so rules you wrote yourself are "
                + "safe, and a malformed marker makes the tool refuse to write rather than guess.");

            painter.Note(
                "FlowIoC offers to install the block the first time you open a project, and to "
                + "refresh it when the rules change. Declining is remembered until the rules "
                + "themselves change. Removing FlowIoC through the Package Manager takes the block "
                + "with it.");
        }
    }
}

#endif
