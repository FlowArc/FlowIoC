#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class ConnectorsPage : HelpPage
    {
        public ConnectorsPage() : base(Build())
        {
        }

        public override string Title => "Connectors";

        public override string Icon => "Linked";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A module never reaches into another module. No type from Modules.A appears in "
                + "Modules.B, and the only crossing point is a Connector.");
            painter.Paragraph(
                "A Connector sub-context binds both signal holders and wires one module's Outgoing to "
                + "another's Incoming. Neither module learns that the other exists.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.SubHeading("The three exceptions");
            painter.Bullet("A Service crosses directly: reference its assembly and inject its interface.");
            painter.Bullet("A sub-module may use its parent's types. The direction is one way.");
            painter.Bullet("A test module under zTestModules may reference anything, wrapped in #if UNITY_EDITOR.");
            painter.Note(
                "Connect also takes a plain delegate, and can adapt between signals whose parameter "
                + "types differ by taking a converter as its second argument.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("hero", "HeroModule", "Outgoing.DecreaseCurrency", 0, 0),
                new HelpGraphNode("connector", "HeroConnectorSubContext", "the only crossing point", 0, 1),
                new HelpGraphNode("profile", "PlayerProfileModule", "Incoming.DecreaseCurrency", 0, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("hero", "connector", "announces"),
                new HelpGraphEdge("connector", "profile", "delivers")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("hero",
                    "The Hero module announces what happened to it. It does not know who cares.",
                    "public class HeroSignalsOutgoing\n{\n    public Signal<double> DecreaseCurrency = new();\n}"),
                new HelpGraphStep("connector",
                    "The Connector binds both signal holders and joins one module's Outgoing to another's Incoming.",
                    "public class HeroConnectorSubContext : Context\n{\n    public override void Setup()\n    {\n        _heroSignals          = InjectionBinderCrossContext.Bind<HeroSignals>();\n        _playerProfileSignals = InjectionBinderCrossContext.Bind<PlayerProfileSignals>();\n\n        _heroSignals.Outgoing.DecreaseCurrency\n            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);\n    }\n}"),
                new HelpGraphStep("profile",
                    "The receiving module answers an ordinary incoming signal. It has no idea a Hero module exists.",
                    "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n    .ToSequence<DecreaseCurrencyCommand>();")
            };

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif
