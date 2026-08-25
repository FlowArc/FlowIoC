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
                + "logs what it wrote. The window is for seeing what is there and for putting a "
                + "deleted one back - which the next Editor session would do anyway.");

            painter.SubHeading("What ships today");
            painter.Bullet(
                "flowioc-data-types - the CD_, RD_, PD_, ED_ and DD_ prefixes, the VO suffix family "
                + "that goes with them, and which folder each kind belongs in.");

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
