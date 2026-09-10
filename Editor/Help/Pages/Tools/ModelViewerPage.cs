#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ModelViewerPage : HelpPage
    {
        public ModelViewerPage() : base(null)
        {
        }

        public override string Title => "Model Viewer";

        public override FlowIcon Icon => FlowIcon.Eye;

        protected override string BodyHeadline => "A Model is a plain C# object, so the Inspector cannot show it.";

        protected override string BodyTagline =>
            "Tools > FlowIoC > Model Viewer shows the live contents of your models while the game "
            + "runs, which is the window a Model would otherwise never have.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("Choosing what to show");
            painter.Code(
                "public class CameraModel : ICameraModel\n"
                + "{\n"
                + "    [ShowInModelViewer] private readonly Dictionary<string, CameraCVO> _cameras = new();\n"
                + "    [ShowInModelViewer] private CinemachineCamera _activeCamera;\n"
                + "\n"
                + "    [HideInModelViewer] private byte[] _scratchBuffer;\n"
                + "}");
            painter.Paragraph(
                "Mark the fields worth watching and leave the noise out. A model whose state you "
                + "can read while the game runs is usually faster to reason about than the same "
                + "state reconstructed from log lines.");
        }
    }
}

#endif