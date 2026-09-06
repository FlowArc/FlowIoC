using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HelpTabTests
    {
        /// <summary>
        /// Every page reads through tabs, even the ones that offer a single reading - the window
        /// draws the bar only when there is more than one, so a page is never left with a tab bar
        /// holding one tab.
        /// </summary>
        [Test]
        public void Every_page_offers_at_least_the_introduction()
        {
            HelpPageCatalog catalog = new HelpPageCatalog();

            foreach (IHelpPage page in catalog.Pages)
            {
                Assert.GreaterOrEqual(page.Tabs.Count, 1, $"'{page.Title}' has no tabs at all.");
                Assert.AreEqual("Introduction", page.Tabs[0].Title,
                    $"'{page.Title}' does not open on its introduction.");
            }
        }

        [Test]
        public void No_page_names_one_tab_twice()
        {
            HelpPageCatalog catalog = new HelpPageCatalog();

            foreach (IHelpPage page in catalog.Pages)
            {
                List<string> titles = page.Tabs.Select(tab => tab.Title).ToList();

                CollectionAssert.AllItemsAreUnique(titles, $"'{page.Title}' repeats a tab title.");
            }
        }

        /// <summary>
        /// The introduction says what Controllers is made of; the two parts it names carry a
        /// reading each, and the rules stay on the end where a reader who knows both can find
        /// them without scrolling past either.
        /// </summary>
        [Test]
        public void Controllers_reads_as_an_introduction_its_two_parts_and_the_rules()
        {
            ControllersPage page = new ControllersPage();
            List<string> titles = page.Tabs.Select(tab => tab.Title).ToList();

            CollectionAssert.AreEqual(new[] {"Introduction", "Command", "Function", "Rules"}, titles);
        }

        /// <summary>
        /// Every architecture topic reads the same way: an introduction that says what the thing is
        /// made of, a reading per part, and the rules last. A reader who has learned one of these
        /// pages knows where to look on the other five.
        /// </summary>
        [Test]
        public void Every_structure_page_opens_on_an_introduction_and_ends_on_the_rules()
        {
            var pages = new IHelpPage[]
            {
                new RootContextPage(),
                new SignalsPage(),
                new ControllersPage(),
                new ModelPage(),
                new ViewMediatorPage(),
                new ConnectorsPage()
            };

            foreach (IHelpPage page in pages)
            {
                List<string> titles = page.Tabs.Select(tab => tab.Title).ToList();

                Assert.GreaterOrEqual(titles.Count, 2,
                    $"'{page.Title}' offers a single reading, so it carries no rules of its own.");
                Assert.AreEqual("Introduction", titles[0],
                    $"'{page.Title}' does not open on its introduction.");
                Assert.AreEqual("Rules", titles[titles.Count - 1],
                    $"'{page.Title}' does not end on its rules.");
            }
        }

        /// <summary>
        /// A page opens on its diagram, so every architecture topic has one to open on. A topic
        /// that could not be drawn would be a topic the reader has to build the picture of alone.
        /// </summary>
        [Test]
        public void Every_structure_page_carries_a_diagram_drawn_larger_than_the_default()
        {
            var pages = new IHelpPage[]
            {
                new RootContextPage(),
                new SignalsPage(),
                new ControllersPage(),
                new ModelPage(),
                new ViewMediatorPage(),
                new ConnectorsPage()
            };

            foreach (IHelpPage page in pages)
            {
                Assert.IsNotNull(page.Graph, $"'{page.Title}' has no diagram.");
                Assert.Greater(page.Graph.Scale, 1f,
                    $"'{page.Title}' draws its diagram at the size a page uses for a second picture.");
            }
        }
    }
}