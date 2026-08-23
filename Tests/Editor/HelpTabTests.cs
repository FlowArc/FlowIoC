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

        [Test]
        public void Controllers_carries_the_rules_beside_its_introduction()
        {
            ControllersPage page = new ControllersPage();
            List<string> titles = page.Tabs.Select(tab => tab.Title).ToList();

            CollectionAssert.AreEqual(new[] { "Introduction", "Rules" }, titles);
        }
    }
}
