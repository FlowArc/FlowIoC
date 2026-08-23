#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class ModelPage : HelpPage
    {
        public ModelPage() : base(Build())
        {
        }

        public override string Title => "Model";

        public override string Icon => "ScriptableObject Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A Model owns state and the rules that keep it valid. It knows nothing about Views, "
                + "Commands, or any other module - which is what makes it the one place worth reading "
                + "when you want to know what a module actually holds.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "A Model may dispatch its own module's outgoing signals to announce that a value it "
                + "holds has changed. Announcing is allowed; listening is not.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("signal", "Incoming Signal", "never arrives here directly", 0, 0),
                new HelpGraphNode("command", "Command", "the only way in", 0, 1),
                new HelpGraphNode("model", "PlayerModel", "state and its rules", 0, 2),
                new HelpGraphNode("outgoing", "Outgoing Signal", "the value changed", 1, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("signal", "command", "runs"),
                new HelpGraphEdge("command", "model", "calls"),
                new HelpGraphEdge("model", "outgoing", "announces"),
                new HelpGraphEdge("signal", "model", "never", HelpGraphEdgeKind.Forbidden)
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("command",
                    "A Model never subscribes to a signal. An incoming signal runs a Command, and the Command calls the Model.",
                    "public override void Execute() => _playerModel.AddCurrency(_amount);"),
                new HelpGraphStep("model",
                    "The Model keeps its own state valid. Nothing reaches in and sets a field from outside.",
                    "public class PlayerModel : IPlayerModel\n{\n    public double Currency { get; private set; }\n\n    public void AddCurrency(double amount) => Currency += amount;\n}"),
                new HelpGraphStep("outgoing",
                    "A Model may dispatch its own module's outgoing signals. Announcing is allowed; listening is not.",
                    "_signals.Outgoing.CurrencyChanged.Dispatch(Currency);")
            };

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif
