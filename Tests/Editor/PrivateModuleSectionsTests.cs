using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PrivateModuleSectionsTests
    {
        private class NamedPage : PrivateModulePage
        {
            private readonly string _title;

            internal NamedPage(string title) => _title = title;

            public override string Title => _title;

            public override string ModuleFolderName => _title + "Module";

            public override void DrawBody(HelpPainter painter)
            {
            }
        }

        private static HelpSection CategoryOf(params string[] titles) =>
            new PrivateModuleSections(titles.Select(title => (PrivateModulePage) new NamedPage(title))
                .ToList()).Category();

        /// <summary>
        /// A project without a private package is a project that sees the help window it has
        /// always seen. An empty category would be a heading with nothing under it.
        /// </summary>
        [Test]
        public void No_private_page_means_no_category_at_all()
        {
            Assert.IsNull(new PrivateModuleSections(new List<PrivateModulePage>()).Category());
        }

        [Test]
        public void One_private_page_makes_a_category_holding_it()
        {
            HelpSection category = CategoryOf("Ads");

            Assert.IsTrue(category.IsCategory);
            Assert.AreEqual("Private Modules", category.Title);
            CollectionAssert.AreEqual(
                new[] {"Ads"}, category.Children.Select(child => child.Title).ToList());
        }

        /// <summary>
        /// The order is the reader's, not the compiler's. TypeCache answers in whatever order the
        /// assemblies were scanned, which would move rows around between reloads.
        /// </summary>
        [Test]
        public void The_pages_are_listed_by_title()
        {
            HelpSection category = CategoryOf("Save", "Ads", "Notifications");

            CollectionAssert.AreEqual(
                new[] {"Ads", "Notifications", "Save"},
                category.Children.Select(child => child.Title).ToList());
        }

        [Test]
        public void The_category_names_an_icon()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(CategoryOf("Ads").Icon));
        }

        /// <summary>
        /// A page is a class of its own. Every test in this assembly declares its doubles as
        /// nested classes, so a scan that picked those up would put them in the help window of
        /// every project that has the test assembly loaded - which is all of them.
        /// </summary>
        [Test]
        public void A_page_nested_inside_another_class_is_not_collected()
        {
            HelpSection category = new PrivateModuleSections().Category();

            if (category == null)
                return;

            CollectionAssert.DoesNotContain(
                category.Children.Select(child => child.Title).ToList(), "Inspector Extras");
        }
    }
}
