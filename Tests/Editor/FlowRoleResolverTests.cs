using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.Editor.Inspector;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowRoleResolverTests
    {
        [FlowHeader(FlowRole.Adapter)]
        private class DecoratedView : IView
        {
            public bool IsRegistered { get; set; }
            public Transform transform => null;
            public GameObject gameObject => null;
        }

        private class PlainView : IView
        {
            public bool IsRegistered { get; set; }
            public Transform transform => null;
            public GameObject gameObject => null;
        }

        private class CounterService { }

        private class MapSystem { }

        private class Loose { }

        private class PlayerRoot { }

        [FlowHeader(FlowRole.Adapter, "Screen Manager")]
        private class Renamed { }

        [Test]
        public void TryResolve_prefers_the_attribute_over_the_interface()
        {
            bool found = new FlowRoleResolver().TryResolve(typeof(DecoratedView), out FlowRole role);

            Assert.IsTrue(found);
            Assert.AreEqual(FlowRole.Adapter, role);
        }

        [Test]
        public void TryResolve_reads_a_view_from_its_interface()
        {
            bool found = new FlowRoleResolver().TryResolve(typeof(PlainView), out FlowRole role);

            Assert.IsTrue(found);
            Assert.AreEqual(FlowRole.View, role);
        }

        [Test]
        public void TryResolve_reads_a_service_from_its_name()
        {
            new FlowRoleResolver().TryResolve(typeof(CounterService), out FlowRole role);

            Assert.AreEqual(FlowRole.Service, role);
        }

        [Test]
        public void TryResolve_reads_a_system_from_its_name()
        {
            new FlowRoleResolver().TryResolve(typeof(MapSystem), out FlowRole role);

            Assert.AreEqual(FlowRole.System, role);
        }

        [Test]
        public void TryResolve_answers_false_for_a_type_that_is_not_ours()
        {
            bool found = new FlowRoleResolver().TryResolve(typeof(Loose), out _);

            Assert.IsFalse(found);
        }

        private class ScreenServiceRoot : FlowIoC.BaseModule.Root.RootBase { }

        private class InventoryRoot : FlowIoC.BaseModule.Root.RootBase { }

        private class MainConnectorRoot : FlowIoC.BaseModule.Root.RootBase { }

        [Test]
        public void TryResolve_gives_a_root_the_colour_of_what_it_roots()
        {
            var resolver = new FlowRoleResolver();

            resolver.TryResolve(typeof(ScreenServiceRoot), out FlowRole service);
            resolver.TryResolve(typeof(MainConnectorRoot), out FlowRole connector);
            resolver.TryResolve(typeof(InventoryRoot), out FlowRole plain);

            Assert.AreEqual(FlowRole.Service, service);
            Assert.AreEqual(FlowRole.Connector, connector);
            Assert.AreEqual(FlowRole.Root, plain);
        }

        private class LocalSaveTestRoot : FlowIoC.BaseModule.Root.RootBase { }

        /// <summary>
        /// A test module's Root wears the Test colour, so a scene says which Roots are there to
        /// exercise a module and which are the module. The rule is the Root's alone: a View in a
        /// test module is still a View.
        /// </summary>
        [Test]
        public void TryResolve_gives_a_test_modules_root_the_test_colour()
        {
            var resolver = new FlowRoleResolver();

            resolver.TryResolve(typeof(LocalSaveTestRoot), out FlowRole role);

            Assert.AreEqual(FlowRole.Test, role);
        }

        [Test]
        public void LabelFor_still_says_root_when_a_root_wears_another_colour()
        {
            var resolver = new FlowRoleResolver();

            Assert.AreEqual("SERVICE · ROOT", resolver.LabelFor(typeof(ScreenServiceRoot), FlowRole.Service));
            Assert.AreEqual("ROOT", resolver.LabelFor(typeof(InventoryRoot), FlowRole.Root));
        }

        [Test]
        public void LabelFor_prefers_the_label_the_attribute_names()
        {
            Assert.AreEqual("VIEW INJECTOR", new FlowRoleResolver().LabelFor(typeof(Labelled), FlowRole.Mediator));
        }

        [FlowHeader(FlowRole.Mediator, label: "View Injector")]
        private class Labelled { }

        [Test]
        public void TitleFor_spaces_out_the_type_name()
        {
            Assert.AreEqual("PLAYER ROOT", new FlowRoleResolver().TitleFor(typeof(PlayerRoot)));
        }

        [Test]
        public void TitleFor_prefers_the_title_the_attribute_names()
        {
            Assert.AreEqual("SCREEN MANAGER", new FlowRoleResolver().TitleFor(typeof(Renamed)));
        }
    }
}
