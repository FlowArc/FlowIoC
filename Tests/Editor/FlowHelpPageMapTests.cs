using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Inspector;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowHelpPageMapTests
    {
        [Test]
        public void PageFor_maps_a_root_to_the_root_and_context_page()
        {
            Assert.AreEqual("Root & Context", new FlowHelpPageMap().PageFor(FlowRole.Root));
        }

        [Test]
        public void PageFor_maps_both_halves_of_the_pair_to_the_same_page()
        {
            var map = new FlowHelpPageMap();

            Assert.AreEqual("View & Mediator", map.PageFor(FlowRole.View));
            Assert.AreEqual("View & Mediator", map.PageFor(FlowRole.Mediator));
        }

        [Test]
        public void PageFor_answers_null_for_a_role_with_no_page()
        {
            var map = new FlowHelpPageMap();

            Assert.IsNull(map.PageFor(FlowRole.Service));
            Assert.IsNull(map.PageFor(FlowRole.System));
        }

        [Test]
        public void Every_mapped_page_exists_in_the_catalogue()
        {
            var map = new FlowHelpPageMap();
            var catalog = new HelpPageCatalog();

            foreach (FlowRole role in Enum.GetValues(typeof(FlowRole)))
            {
                string title = map.PageFor(role);

                if (title == null)
                    continue;

                Assert.IsNotNull(catalog.FindPage(title), $"{role} points at a page that does not exist: {title}");
            }
        }
    }
}
