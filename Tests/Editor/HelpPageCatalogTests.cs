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
        /// What the sidebar shows with nothing opened: the introduction, everything there is to
        /// know about FlowIoC, and the modules that ship with it. A reader picks which of the
        /// three they are here for before picking a topic inside it.
        /// </summary>
        [Test]
        public void The_sidebar_rests_on_one_topic_and_two_categories()
        {
            List<string> titles = _catalog.Sections.Select(section => section.Title).ToList();

            CollectionAssert.AreEqual(new[] {"Welcome", "Wiki", "Modules"}, titles);

            Assert.IsFalse(_catalog.Sections[0].IsCategory);
            Assert.IsTrue(_catalog.Sections[1].IsCategory);
            Assert.IsTrue(_catalog.Sections[2].IsCategory);
        }

        /// <summary>
        /// Wiki is the whole reference. Its own topics come first, because they are what a reader
        /// does before any of the rest applies, and the two categories that go deeper follow.
        /// </summary>
        [Test]
        public void The_wiki_category_opens_on_its_topics_and_holds_the_deeper_categories()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Creating a Module",
                "Folder Layout",
                "Data Types",
                "Structure",
                "Editor Tools"
            }, ChildTitles("Wiki"));
        }

        [Test]
        public void The_structure_category_covers_the_architecture()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Root & Context",
                "Signals",
                "Controllers",
                "Model",
                "View & Mediator",
                "Connectors"
            }, ChildTitles("Structure"));
        }

        [Test]
        public void The_editor_tools_category_covers_every_window_the_package_adds()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Code Generators",
                "Module Configuration",
                "Flow Console",
                "Model Viewer",
                "Folder Drawer",
                "Screen Config Manager",
                "Agent Rules",
                "Agent Skills"
            }, ChildTitles("Editor Tools"));
        }

        [Test]
        public void The_modules_category_lists_the_modules_that_ship()
        {
            CollectionAssert.AreEqual(
                new[] {"Setup Modules", "Countdown Service", "Camera System", "Input"}, ChildTitles("Modules"));
        }

        /// <summary>
        /// The categories nest, and the window remembers which are open by the path down to them.
        /// A page two levels deep has to name both of them or it could never be reached.
        /// </summary>
        [Test]
        public void A_nested_page_names_every_category_above_it()
        {
            IHelpPage page = _catalog.Pages.First(candidate => candidate.Title == "Signals");

            CollectionAssert.AreEqual(new[] {"Wiki", "Structure"},
                _catalog.CategoriesContaining(page).Select(section => section.Title).ToList());
        }

        /// <summary>
        /// The introduction sits at the top level, so the sidebar has nothing to fold open when
        /// the window opens - which is what leaves it resting on its three entries.
        /// </summary>
        [Test]
        public void The_opening_page_sits_inside_no_category()
        {
            CollectionAssert.IsEmpty(_catalog.CategoriesContaining(_catalog.OpeningPage).ToList());
        }

        /// <summary>
        /// The window opens on the introduction, not on whichever page happens to be first and
        /// not on a topic folded away inside a category.
        /// </summary>
        [Test]
        public void The_window_opens_on_the_introduction()
        {
            Assert.AreEqual("Welcome", _catalog.OpeningPage.Title);
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

        /// <summary>
        /// The Tools/FlowIoC/Help menu has an entry per top level section, and each opens the
        /// window somewhere a reader can start from. A category opens on the first topic inside
        /// it, however deep that topic sits.
        /// </summary>
        [Test]
        public void A_category_starts_at_the_first_topic_inside_it()
        {
            Assert.AreEqual("Creating a Module", _catalog.FirstPageOf("Wiki")?.Title);
            Assert.AreEqual("Setup Modules", _catalog.FirstPageOf("Modules")?.Title);
        }

        [Test]
        public void A_section_that_is_a_topic_starts_at_itself()
        {
            Assert.AreEqual("Welcome", _catalog.FirstPageOf("Welcome")?.Title);
        }

        /// <summary>
        /// Only the top level is offered a menu entry, so a category nested inside one is not
        /// found here even though the sidebar shows it.
        /// </summary>
        [Test]
        public void A_section_that_is_not_at_the_top_level_has_no_starting_page()
        {
            Assert.IsNull(_catalog.FirstPageOf("Structure"));
            Assert.IsNull(_catalog.FirstPageOf("Nothing By This Name"));
        }

        /// <summary>
        /// What one category shows when it folds open, in order - topics and sub categories
        /// alike. The category is looked up anywhere in the tree, because Structure and Editor
        /// Tools live inside Wiki rather than beside it.
        /// </summary>
        private List<string> ChildTitles(string category) =>
            _catalog.Sections
                .SelectMany(section => section.Descendants())
                .First(section => section.Title == category)
                .Children.Select(child => child.Title)
                .ToList();
    }
}