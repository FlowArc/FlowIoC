using System.Linq;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModelPageTests
    {
        /// <summary>
        /// "A Model never subscribes to a signal" is a rule the diagram states by drawing the route
        /// and crossing it out. Losing that edge turns the page into a diagram of a rule nobody is
        /// told about.
        /// </summary>
        [Test]
        public void The_page_draws_the_route_a_Model_may_not_take()
        {
            ModelPage page = new ModelPage();

            HelpGraphEdge forbidden = page.Graph.Edges
                .FirstOrDefault(edge => edge.Kind == HelpGraphEdgeKind.Forbidden);

            Assert.IsNotNull(forbidden, "The Model page no longer shows the forbidden route.");
            Assert.AreEqual("model", forbidden.ToId);
        }
    }
}
