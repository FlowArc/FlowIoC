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
            painter.SubHeading("What it lists");
            painter.Paragraph(
                "One tree. A row per Root in the scene, in Initialize Order and in the colour of the "
                + "role its Root plays; under it everything that module bound - its Models, its Service "
                + "or System, its sub services - each badged with what its name says it is; under each "
                + "of those the members it chose to show. A sub context gets a row of its own when it "
                + "bound something. The last row, Shared, is what the RootsManager bound before any "
                + "Root: the shared data model, with every asset and component filed under its name.");
            painter.Paragraph(
                "Left out: the framework's own plumbing, the GameObject and provider entries, and the "
                + "module's signal holders - those are its surface, not its state.");

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
                "A public field or property is listed unless it is marked [HideInModelViewer]; a "
                + "private one only when it is marked [ShowInModelViewer]. Base classes count. Static "
                + "members, indexers and setter-only properties never appear. Mark the fields worth "
                + "watching and leave the noise out: a model whose state you can read while the game "
                + "runs is usually faster to reason about than the same state reconstructed from log lines.");

            painter.SubHeading("Reading a row");
            painter.Bullet(
                "A row that folds is opened by clicking anywhere on it. Only an open row is read, ten "
                + "times a second, so a closed one costs nothing however deep the data under it goes.");
            painter.Bullet(
                "The value cell writes a leaf out - a number, a quoted string, an enum, a Vector - and "
                + "for a collection says how many items it holds; the column at the right is the declared "
                + "type. An object with a ToString of its own is described by it.");
            painter.Bullet(
                "A dictionary opens to its entries named by their keys, a list to [0], [1] and so on, "
                + "fifty at a time with a row offering the rest.");
            painter.Bullet(
                "A UnityEngine.Object is shown by name and type; clicking its row pings it.");
            painter.Bullet(
                "A member whose getter throws is a red row carrying the exception, and the rest of the "
                + "window carries on. A value that is already open higher on the same path says so "
                + "instead of opening again.");
            painter.Note(
                "The window reads and never writes. A value changed from a window would skip the rules "
                + "the Model exists to keep - so a value is changed where it is always changed, by a "
                + "Command through the Model.");
        }
    }
}

#endif