using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HelpTreeNodeTests
    {
        [Test]
        public void A_leaf_carries_its_name_and_its_comment()
        {
            HelpTreeNode leaf = new HelpTreeNode("Models", "state and the rules that keep it valid");

            Assert.AreEqual("Models", leaf.Name);
            Assert.AreEqual("state and the rules that keep it valid", leaf.Comment);
            Assert.AreEqual(0, leaf.Children.Count);
        }

        /// <summary>
        /// The folder layout page is checked against the module generator's own folder list, and
        /// that check walks the whole tree rather than its top level.
        /// </summary>
        [Test]
        public void Descendants_reaches_every_level()
        {
            HelpTreeNode root = new HelpTreeNode("Scripts", "",
                new HelpTreeNode("Runtime", "",
                    new HelpTreeNode("Models", "state")));

            List<string> names = root.Descendants().Select(node => node.Name).ToList();

            CollectionAssert.AreEquivalent(new[] { "Scripts", "Runtime", "Models" }, names);
        }
    }
}
