#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class FolderPainterPage : HelpPage
    {
        public FolderPainterPage() : base(null)
        {
        }

        public override string Title => "Folder Painter";

        public override FlowIcon Icon => FlowIcon.Brush;

        protected override string BodyHeadline => "Tinted folders keep a large module tree readable.";

        protected override string BodyTagline =>
            "Tools > FlowIoC > Folder Painter colours the Project window by rule rather than folder "
            + "by folder, and an edit repaints the window as you make it.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("Two kinds of rule");
            painter.Bullet(
                "Path rules match on the folder path - contains, ends with or starts with. They "
                + "are checked in order and the first match wins, so put the specific ones first. "
                + "A rule ending with Module colours every generated module without naming any.");
            painter.Bullet(
                "Folder rules point at one folder asset and take priority over the path rules. Use "
                + "them for the handful of folders you want to stand out individually.");
            painter.Paragraph(
                "Each rule sets a gradient, and optionally a label override, a selection colour and "
                + "an icon.");

            painter.Note(
                "The settings live in your project rather than in the package, so the colours are "
                + "per project. A sensible default set is created the first time the Editor opens.");
        }
    }
}

#endif