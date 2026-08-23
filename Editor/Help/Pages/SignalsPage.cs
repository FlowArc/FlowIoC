#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class SignalsPage : HelpPage
    {
        public SignalsPage() : base(Build())
        {
        }

        public override string Title => "Signals";

        public override string Icon => "Profiler.NetworkMessages";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A signal is a name and a payload. Incoming is what the module accepts, Outgoing is "
                + "what it announces. Together they are the module's whole public surface - the only "
                + "other thing another module may reference directly is the interface of a Service.");
            painter.Paragraph(
                "Because the surface is this narrow, a module can be rewritten from the inside "
                + "without anything else in the game noticing.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.SubHeading("The holder");
            painter.Code(
                "public class PlayerSignals : ISignalHolder\n"
                + "{\n"
                + "    public PlayerSignalsIncoming Incoming = new();\n"
                + "    public PlayerSignalsOutgoing Outgoing = new();\n"
                + "}");
            painter.Paragraph(
                "Signals come in five arities, from Signal to Signal<T1, T2, T3, T4>, and every one "
                + "of them takes direct listeners as well as command bindings.");
            painter.Code(
                "_signals.Incoming.AddCurrency.AddListener(OnCurrencyAdded);\n"
                + "_signals.Incoming.AddCurrency.AddListenerOnce(OnFirstCurrencyOnly);\n"
                + "_signals.Incoming.AddCurrency.RemoveListener(OnCurrencyAdded);\n"
                + "\n"
                + "_signals.Incoming.AddCurrency.Dispatch(100d);");
            painter.Note(
                "A signal that must never leave the module belongs in a separate PlayerSignalsInternal "
                + "holder, bound with the plain InjectionBinder rather than the cross-context one.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("incoming", "Incoming", "what the module accepts", 0, 0),
                new HelpGraphNode("module", "PlayerModule", "state, logic, presentation", 0, 1),
                new HelpGraphNode("outgoing", "Outgoing", "what the module announces", 0, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("incoming", "module", "runs a command"),
                new HelpGraphEdge("module", "outgoing", "dispatches")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("incoming",
                    "Incoming is what the module accepts. Each one is bound to a command in the Context.",
                    "public class PlayerSignalsIncoming\n{\n    public Signal InitializePlayer = new();\n    public Signal<double> AddCurrency = new();\n}"),
                new HelpGraphStep("outgoing",
                    "Outgoing is what the module announces. Who listens is decided elsewhere, in a Connector.",
                    "public class PlayerSignalsOutgoing\n{\n    public Signal<double> CurrencyChanged = new();\n}")
            };

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif
