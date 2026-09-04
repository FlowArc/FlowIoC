#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class AgentScannerPage : HelpPage
    {
        public AgentScannerPage() : base(null)
        {
        }

        public override string Title => "Agent Scanner";

        public override string Icon => "console.infoicon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Agent Scanner. Everything this project tells an AI coding "
                + "assistant, in one list: the rule block in AGENTS.md and CLAUDE.md, and the skill "
                + "folders under .claude/skills. Both are files FlowIoC owns, so the question about "
                + "either one is the same - is it there, and does it describe the version the "
                + "project is on.");
            painter.Paragraph(
                "A row is green while its file is current, amber while it is missing or out of "
                + "date, and red when only a person can settle it - a marker somebody has broken, "
                + "a file that could not be written. Sync writes everything amber; nothing it does "
                + "clears a red row.");

            painter.SubHeading("Agent rules");
            painter.Paragraph(
                "FlowIoC imposes an architecture that nothing in the C# type system enforces, so an "
                + "assistant that has not been told the rules will happily write code that compiles "
                + "and violates every one of them - logic in a Context, one module injecting "
                + "another's model, Inject on a field where it is silently skipped.");
            painter.Paragraph(
                "The rules go into the project's root AGENTS.md - the convention Claude Code, Codex, "
                + "Cursor, Zed and Gemini CLI all read - and CLAUDE.md is pointed at that file. They "
                + "land inside a marked block:");
            painter.Code(
                "<!-- FLOWIOC:BEGIN version=<installed> hash=<rule text> | ... -->\n"
                + "...\n"
                + "<!-- FLOWIOC:END -->");
            painter.Paragraph(
                "Nothing outside those markers is ever touched, so rules you wrote yourself are "
                + "safe, and a malformed marker makes the tool refuse to write rather than guess. "
                + "The rule text ships in Documentation~/AgentRules.md.");

            painter.SubHeading("Agent skills");
            painter.Paragraph(
                "The rules are what an assistant is told on every task, so they stay short. A skill "
                + "is what it reaches for when one particular kind of work comes up, and it can "
                + "afford to be longer. One folder per skill, under .claude/skills.");
            painter.Bullet(
                "flowioc-data-types - the CD_, RD_, PD_, ED_ and DD_ prefixes, the VO suffix family "
                + "that goes with them, and which folder each kind belongs in.");
            painter.Bullet(
                "flowioc-scaffolding - which menu item lays a module out and what to tick in it, "
                + "why the optional folders matter, where the .csproj.DotSettings files land, and "
                + "how to drive the generators from a terminal against an open Editor.");

            painter.SubHeading("What writes itself");
            painter.Paragraph(
                "You do not have to press Sync. FlowIoC writes whatever is missing or out of date "
                + "when the Editor opens, and again whenever it falls behind the version the "
                + "project is on, so updating the package updates both with it. What it wrote is "
                + "logged, so a folder appearing under .claude is never a mystery.");
            painter.Paragraph(
                "A project that would rather decide for itself unticks one of the two switches at "
                + "the foot of the window, and then nothing of that kind is written until Sync is "
                + "pressed. The switches are remembered per project and are separate from each "
                + "other, so a project may take the rules and refuse the skills.");

            painter.SubHeading("On the way out");
            painter.Paragraph(
                "Removing FlowIoC through the Package Manager takes the block and the shipped "
                + "skills with it, file by file. Nobody asked for them, so nobody should be left "
                + "explaining them.");
            painter.Paragraph(
                "A package removed some other way - manifest.json edited by hand, or the folder "
                + "deleted - raises no event for FlowIoC to act on. Every shipped skill opens by "
                + "saying so: it applies only while FlowIoC is installed, and names the check and "
                + "the folder to delete if it is not.");

            painter.Note(
                "Only the files the package owns are ever compared, written or deleted. A skill "
                + "you wrote yourself is never touched, and a note left beside a shipped skill "
                + "survives both an install and an uninstall - it keeps its folder alive.");
        }
    }
}

#endif
