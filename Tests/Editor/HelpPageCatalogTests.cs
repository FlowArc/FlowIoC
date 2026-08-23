using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HelpPageCatalogTests
    {
        private HelpPageCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = new HelpPageCatalog();

        [Test]
        public void Every_page_has_a_title()
        {
            foreach (IHelpPage page in _catalog.Pages)
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Title));
        }

        [Test]
        public void No_two_pages_share_a_title()
        {
            List<string> titles = _catalog.Pages.Select(page => page.Title).ToList();

            CollectionAssert.AllItemsAreUnique(titles);
        }

        [Test]
        public void Every_page_names_an_icon()
        {
            foreach (IHelpPage page in _catalog.Pages)
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Icon), $"'{page.Title}' has no icon.");
        }

        /// <summary>
        /// What the sidebar shows at rest: two topics on their own, then the two categories.
        /// </summary>
        [Test]
        public void The_sidebar_lists_two_topics_and_two_categories()
        {
            List<string> titles = _catalog.Sections.Select(section => section.Title).ToList();

            CollectionAssert.AreEqual(new[] {"Welcome", "Folder Layout", "Structure", "Editor Tools"}, titles);

            Assert.IsFalse(_catalog.Sections[0].IsCategory);
            Assert.IsFalse(_catalog.Sections[1].IsCategory);
            Assert.IsTrue(_catalog.Sections[2].IsCategory);
            Assert.IsTrue(_catalog.Sections[3].IsCategory);
        }

        [Test]
        public void The_structure_category_covers_the_architecture()
        {
            List<string> titles = Titles("Structure");

            CollectionAssert.AreEqual(new[]
            {
                "Root & Context",
                "Signals",
                "Controllers",
                "Model",
                "View & Mediator",
                "Connectors"
            }, titles);
        }

        [Test]
        public void The_editor_tools_category_covers_every_window_the_package_adds()
        {
            List<string> titles = Titles("Editor Tools");

            CollectionAssert.AreEqual(new[]
            {
                "Code Generators",
                "Module Configuration",
                "Flow Console",
                "Model Viewer",
                "Folder Drawer",
                "Screen Config Manager",
                "Agent Rules"
            }, titles);
        }

        /// <summary>
        /// The window's first job is to explain the folder tree, so that is where it opens - not
        /// on whichever page happens to be first.
        /// </summary>
        [Test]
        public void The_window_opens_on_the_folder_layout()
        {
            Assert.AreEqual("Folder Layout", _catalog.OpeningPage.Title);
        }

        [Test]
        public void The_opening_page_is_one_of_the_pages_on_offer()
        {
            CollectionAssert.Contains(_catalog.Pages.ToList(), _catalog.OpeningPage);
        }

        [Test]
        public void Every_page_belongs_to_exactly_one_section()
        {
            foreach (IHelpPage page in _catalog.Pages)
            {
                int owners = _catalog.Sections.Count(section => section.Contains(page));

                Assert.AreEqual(1, owners, $"'{page.Title}' belongs to {owners} sections.");
            }
        }

        private List<string> Titles(string category) =>
            _catalog.Sections.First(section => section.Title == category)
                .Pages.Select(page => page.Title).ToList();
    }
}