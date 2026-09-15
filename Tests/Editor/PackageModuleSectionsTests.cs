using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Icons;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PackageModuleSectionsTests
    {
        private static readonly ModuleGroup Own = new ModuleGroup("com.flowarc.flowioc.core", "FlowModules", true);
        private static readonly ModuleGroup Addons = new ModuleGroup("com.flowarc.flowioc.addons", "FlowIoC-addons Modules", false);
        private static readonly ModuleGroup Game = new ModuleGroup("com.studio.game", "Game Modules", false);

        private class NamedPage : ModulePage
        {
            private readonly string _title;

            internal NamedPage(string title, ModuleGroup group)
            {
                _title = title;
                Group = group;
            }

            internal ModuleGroup Group { get; }

            public override string Title => _title;

            public override string ModuleFolderName => _title + "Module";

            public override void DrawBody(HelpPainter painter)
            {
            }
        }

        private static IReadOnlyList<HelpSection> CategoriesOf(params NamedPage[] pages) =>
            new PackageModuleSections(pages, page => ((NamedPage) page).Group).Categories();

        private static List<string> Titles(IEnumerable<HelpSection> sections) =>
            sections.Select(section => section.Title).ToList();

        /// <summary>
        /// A package without a page is a package without a category. An empty category would be
        /// a heading with nothing under it.
        /// </summary>
        [Test]
        public void No_page_means_no_category_at_all()
        {
            CollectionAssert.IsEmpty(CategoriesOf());
        }

        [Test]
        public void The_pages_of_one_package_make_one_category_named_after_it()
        {
            IReadOnlyList<HelpSection> categories = CategoriesOf(
                new NamedPage("Ads", Addons), new NamedPage("Save", Addons));

            Assert.AreEqual(1, categories.Count);
            Assert.IsTrue(categories[0].IsCategory);
            Assert.AreEqual("FlowIoC-addons Modules", categories[0].Title);
            CollectionAssert.AreEqual(new[] {"Ads", "Save"}, Titles(categories[0].Children));
        }

        /// <summary>
        /// The order is the reader's, not the compiler's. TypeCache answers in whatever order the
        /// assemblies were scanned, which would move rows around between reloads.
        /// </summary>
        [Test]
        public void The_pages_inside_a_category_are_listed_by_title()
        {
            IReadOnlyList<HelpSection> categories = CategoriesOf(
                new NamedPage("Save", Addons), new NamedPage("Ads", Addons), new NamedPage("Notifications", Addons));

            CollectionAssert.AreEqual(
                new[] {"Ads", "Notifications", "Save"}, Titles(categories[0].Children));
        }

        /// <summary>
        /// FlowIoC's own modules come first whatever their title sorts to - every project has
        /// them - and the packages a project adds follow by title.
        /// </summary>
        [Test]
        public void FlowIoCs_own_category_comes_first_and_the_rest_follow_by_title()
        {
            IReadOnlyList<HelpSection> categories = CategoriesOf(
                new NamedPage("Ads", Game),
                new NamedPage("Save", Addons),
                new NamedPage("Counter", Own));

            CollectionAssert.AreEqual(
                new[] {"FlowModules", "FlowIoC-addons Modules", "Game Modules"}, Titles(categories));
        }

        [Test]
        public void A_category_is_a_folder()
        {
            Assert.AreEqual(FlowIcon.Folder, CategoriesOf(new NamedPage("Ads", Addons))[0].Icon);
        }

        /// <summary>
        /// The editor assembly declares its group's title, and it is the package this test is
        /// compiled from, so the group is FlowIoC's own.
        /// </summary>
        [Test]
        public void The_editor_assembly_is_the_FlowModules_group()
        {
            ModuleGroup group = ModuleGroup.Of(typeof(PackageModuleSections).Assembly);

            Assert.AreEqual("FlowModules", group.Title);
            Assert.IsTrue(group.IsOwn);
        }

        /// <summary>
        /// An assembly that declares no title is named after its package. The test assembly
        /// declares none and is compiled from the FlowIoC package, whose display name is FlowIoC.
        /// </summary>
        [Test]
        public void An_assembly_that_declares_no_title_is_named_after_its_package()
        {
            ModuleGroup group = ModuleGroup.Of(typeof(PackageModuleSectionsTests).Assembly);

            Assert.AreEqual("FlowIoC Modules", group.Title);
            Assert.AreEqual("com.flowarc.flowioc.core", group.Key);
        }

        /// <summary>
        /// A page is a class of its own. Every test in this assembly declares its doubles as
        /// nested classes, so a scan that picked those up would put them in the help window of
        /// every project that has the test assembly loaded - which is all of them.
        /// </summary>
        [Test]
        public void A_page_nested_inside_another_class_is_not_collected()
        {
            IEnumerable<string> titles = new PackageModuleSections().Categories()
                .SelectMany(category => category.Children)
                .Select(child => child.Title);

            CollectionAssert.DoesNotContain(titles.ToList(), "Ads");
        }
    }
}