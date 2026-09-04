#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class AgentSkillsPage : HelpPage
    {
        public AgentSkillsPage() : base(null)
        {
        }

        public override string Title => "Agent Skills";

        public override string Icon => "console.infoicon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > AI > Agent Skills. The agent rules are what an assistant is told "
                + "on every task; a skill is what it reaches for when a particular one comes up. "
                + "FlowIoC ships its conventions as skills so the rule block stays short and the "
                + "detail is there when it is needed.");
            painter.Paragraph(
                "You do not have to ask for them. FlowIoC writes each shipped skill into this "
                + "project's .claude/skills folder when the Editor opens, one folder per skill, and "
                + "logs what it wrote. It writes them again whenever they fall behind the version "
                + "the project is on, so updating the package updates the skills with it. The "
                + "window is for seeing what is there and for putting a deleted one back - which "
                + "the next Editor session would do anyway.");
            painter.Paragraph(
                "A project that would rather decide for itself turns off Keep the shipped skills "
                + "up to date automatically in the window, and then nothing is written until "
                + "Install is pressed. The switch is remembered per project, and it is separate "
                + "from the one the agent rules carry: a project may take one and refuse the other.");

            painter.SubHeading("What ships today");
            painter.Bullet(
                "flowioc-data-types - the CD_, RD_, PD_, ED_ and DD_ prefixes, the VO suffix family "
                + "that goes with them, and which folder each kind belongs in.");
            painter.Bullet(
                "flowioc-scaffolding - which menu item lays a module out and what to tick in it, "
                + "why the optional folders matter, where the .csproj.DotSettings files land, and "
                + "how to drive the generators from a terminal against an open Editor.");

            painter.SubHeading("On the way out");
            painter.Paragraph(
                "Removing FlowIoC through the Package Manager takes the shipped skills with it, "
                + "file by file. Nobody asked for them, so nobody should be left explaining them.");

            painter.Note(
                "Only the files the package owns are ever compared, written or deleted. A skill "
                + "you wrote yourself is never touched, and a note left beside a shipped skill "
                + "survives both an install and an uninstall - it keeps its folder alive.");
        }
    }
}

#endif
