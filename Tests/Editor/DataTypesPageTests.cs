using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class DataTypesPageTests
    {
        private DataTypesPage _page;

        [SetUp]
        public void SetUp() => _page = new DataTypesPage();

        /// <summary>
        /// The page is the readable half of a convention whose other half is the shipped code
        /// style. If a prefix is declared legal there and never explained here, a reader meets a
        /// name the window cannot account for.
        /// </summary>
        [Test]
        public void Every_prefix_the_code_style_declares_is_shown()
        {
            List<string> shown = Names();

            foreach (string prefix in new[] {"CD_", "RD_", "PD_", "ED_", "DD_"})
            {
                Assert.IsTrue(shown.Any(name => name.StartsWith(prefix)),
                    $"The page explains no asset named with the '{prefix}' prefix.");
            }
        }

        [Test]
        public void Every_value_object_suffix_is_shown()
        {
            List<string> shown = Names();

            foreach (string suffix in new[] {"VO", "CVO", "RVO", "PVO", "EVO", "DVO"})
            {
                Assert.IsTrue(shown.Any(name => name.EndsWith(suffix)),
                    $"The page explains no value object named with the '{suffix}' suffix.");
            }
        }

        /// <summary>
        /// The two folders are where the convention lives; naming the types without naming the
        /// folders would leave a reader with nowhere to put them.
        /// </summary>
        [Test]
        public void Both_data_folders_are_named()
        {
            List<string> shown = Names();

            Assert.Contains("UnityObjects", shown);
            Assert.Contains("ValueObjects", shown);
        }

        [Test]
        public void Every_leaf_the_page_shows_says_what_belongs_in_it()
        {
            foreach (HelpTreeNode node in _page.Root.Descendants())
            {
                if (node.Children.Count == 0)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(node.Comment), $"'{node.Name}' has no comment.");
            }
        }

        [Test]
        public void The_page_offers_a_second_reading()
        {
            CollectionAssert.AreEqual(new[] {"Introduction", "Rules"},
                _page.Tabs.Select(tab => tab.Title).ToList());
        }

        private List<string> Names() => _page.Root.Descendants().Select(node => node.Name).ToList();
    }
}
