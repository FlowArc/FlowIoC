#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class ModelViewerPage : HelpPage
    {
        public ModelViewerPage() : base(null)
        {
        }

        public override string Title => "Model Viewer";

        public override string Icon => "ScriptableObject Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Model Viewer. Shows the live contents of your models while the "
                + "game runs. Models are plain C# objects rather than MonoBehaviours, so the Unity "
                + "Inspector cannot show them - this window is the replacement.");

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
