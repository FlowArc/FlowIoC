using System.Collections.Generic;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Help.Graph;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A diagram is drawn from ids. An edge or a step naming a box that does not exist draws
    /// nothing at all and says nothing about why, so the check belongs in a test rather than in
    /// the painter.
    /// </summary>
    public class HelpCatalogGraphTests
    {
        private HelpPageCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = new HelpPageCatalog();

        [Test]
        public void Every_edge_joins_two_boxes_that_exist()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                if (page.Graph == null)
                    continue;

                foreach (HelpGraphEdge edge in page.Graph.Edges)
                {
                    Assert.IsTrue(page.Graph.HasNode(edge.FromId), $"{page.Title}: no box '{edge.FromId}'.");
                    Assert.IsTrue(page.Graph.HasNode(edge.ToId), $"{page.Title}: no box '{edge.ToId}'.");
                }
            }
        }

        [Test]
        public void Every_step_lights_up_a_box_that_exists()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                if (page.Graph == null)
                    continue;

                foreach (HelpGraphStep step in page.Graph.Steps)
                    Assert.IsTrue(page.Graph.HasNode(step.NodeId), $"{page.Title}: no box '{step.NodeId}'.");
            }
        }

        [Test]
        public void No_two_boxes_on_a_page_share_an_id()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                if (page.Graph == null)
                    continue;

                HashSet<string> seen = new HashSet<string>();

                foreach (HelpGraphNode node in page.Graph.Nodes)
                    Assert.IsTrue(seen.Add(node.Id), $"{page.Title}: '{node.Id}' appears twice.");
            }
        }

        /// <summary>
        /// Two boxes in the same cell are drawn on top of each other.
        /// </summary>
        [Test]
        public void No_two_boxes_on_a_page_share_a_cell()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                if (page.Graph == null)
                    continue;

                HashSet<string> cells = new HashSet<string>();

                foreach (HelpGraphNode node in page.Graph.Nodes)
                {
                    Assert.IsTrue(cells.Add($"{node.Row}:{node.Column}"),
                        $"{page.Title}: two boxes sit in cell {node.Row}:{node.Column}.");
                }
            }
        }

        [Test]
        public void Every_step_carries_a_rule_and_the_code_it_produces()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                if (page.Graph == null)
                    continue;

                foreach (HelpGraphStep step in page.Graph.Steps)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(step.Rule), $"{page.Title}: a step has no rule.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(step.Code), $"{page.Title}: a step has no code.");
                }
            }
        }

        [Test]
        public void The_catalog_lists_root_and_context_and_signals()
        {
            List<string> titles = new List<string>();

            foreach (IHelpPage page in _catalog.Pages)
                titles.Add(page.Title);

            Assert.Contains("Root & Context", titles);
            Assert.Contains("Signals", titles);
        }
    }
}
