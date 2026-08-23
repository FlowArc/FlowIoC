using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class WelcomePageTests
    {
        /// <summary>
        /// The map is the page's answer to "what is FlowIoC". Losing a box quietly turns the round
        /// trip into a shorter story than the one the rules describe.
        /// </summary>
        [Test]
        public void The_map_shows_every_part_of_the_round_trip()
        {
            WelcomePage page = new WelcomePage();
            List<string> ids = page.Graph.Nodes.Select(node => node.Id).ToList();

            foreach (string required in new[]
                     {
                         "root", "incoming", "commands", "views", "outgoing", "state", "connector"
                     })
            {
                Assert.Contains(required, ids);
            }
        }

        /// <summary>
        /// This one is a map rather than a walk. With no steps the painter leaves out the Previous
        /// and Next controls, and nothing on the page claims a step that does not exist.
        /// </summary>
        [Test]
        public void The_map_is_not_a_stepped_walk()
        {
            WelcomePage page = new WelcomePage();

            Assert.AreEqual(0, page.Graph.Steps.Count);
        }
    }
}
