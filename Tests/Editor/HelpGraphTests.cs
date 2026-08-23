using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HelpGraphTests
    {
        private HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("signal", "Incoming Signal", "AddCurrency", 0, 0),
                new HelpGraphNode("command", "Command", "AddCurrencyCommand", 0, 1)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("signal", "command", "runs", HelpGraphEdgeKind.Normal)
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("signal", "A signal is a name and a payload.", "public Signal<double> AddCurrency = new();")
            };

            return new HelpGraph(nodes, edges, steps);
        }

        [Test]
        public void A_node_can_be_found_by_its_id()
        {
            HelpGraph graph = Build();

            Assert.IsTrue(graph.HasNode("command"));
            Assert.AreEqual("Command", graph.Node("command").Title);
        }

        [Test]
        public void An_unknown_id_is_reported_rather_than_thrown()
        {
            HelpGraph graph = Build();

            Assert.IsFalse(graph.HasNode("model"));
            Assert.IsNull(graph.Node("model"));
        }

        /// <summary>
        /// The painter reads Row and Column to place a box. A node that loses them would be
        /// drawn on top of another one, which is invisible in a test but obvious on screen.
        /// </summary>
        [Test]
        public void A_node_keeps_the_cell_it_was_given()
        {
            HelpGraphNode node = new HelpGraphNode("model", "Model", "PlayerModel", 2, 3);

            Assert.AreEqual(2, node.Row);
            Assert.AreEqual(3, node.Column);
        }

        [Test]
        public void An_edge_defaults_to_a_normal_arrow()
        {
            HelpGraphEdge edge = new HelpGraphEdge("signal", "command", "runs");

            Assert.AreEqual(HelpGraphEdgeKind.Normal, edge.Kind);
        }
    }
}
