using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The generated context is the screen's whole declaration, so what the Create Module window
    /// was told has to come out of the template verbatim - and the file has to carry the anchor
    /// lines the other generators insert after.
    /// </summary>
    public class ScreenContextTemplateTests
    {
        private ScreenContextTemplate _template;
        private ScreenModuleSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _template = new ScreenContextTemplate();
            _settings = new ScreenModuleSettings { AddressableKey = "MainScreen" };
        }

        private string Render() => _template.Render(
            "Modules.MainModule.MainScreenModule.RootsContexts", "MainScreenContext", "MainScreenView",
            "MainScreenMediator", "Modules.MainModule.MainScreenModule.ViewsMediators", _settings);

        [Test]
        public void The_context_derives_from_ScreenSubContext_with_the_view_and_mediator()
        {
            StringAssert.Contains(
                "public class MainScreenContext : ScreenSubContext<MainScreenView, MainScreenMediator>", Render());
        }

        [Test]
        public void The_namespace_and_the_view_namespace_are_written()
        {
            string rendered = Render();

            StringAssert.Contains("namespace Modules.MainModule.MainScreenModule.RootsContexts", rendered);
            StringAssert.Contains("using Modules.MainModule.MainScreenModule.ViewsMediators;", rendered);
            StringAssert.Contains("using FlowIoC.ScreenModule.RootsContexts;", rendered);
            StringAssert.Contains("using FlowIoC.ScreenModule.Data;", rendered);
            StringAssert.Contains("using FlowIoC.ScreenModule.Enums;", rendered);
        }

        [Test]
        public void An_addressable_screen_declares_its_address()
        {
            StringAssert.Contains("Load = ScreenLoadCVO.Addressable(\"MainScreen\"),", Render());
        }

        [Test]
        public void A_resource_screen_declares_its_path()
        {
            _settings.LoadType = ScreenLoadType.Resource;
            _settings.ResourcePath = "Screens/Main";

            StringAssert.Contains("Load = ScreenLoadCVO.Resource(\"Screens/Main\"),", Render());
        }

        [Test]
        public void Manager_layer_tag_and_animation_flags_come_through()
        {
            _settings.ManagerId = 1;
            _settings.Layer = 2;
            _settings.Tag = ScreenTag.GroupA;
            _settings.HasShowAnimation = true;

            string block = _template.RenderScreenBlock(_settings);

            StringAssert.StartsWith("protected override ScreenCVO Screen => new()", block.Trim());
            StringAssert.Contains("ManagerId = 1,", block);
            StringAssert.Contains("Layer = 2,", block);
            StringAssert.Contains("Tag = ScreenTag.GroupA,", block);
            StringAssert.Contains("HasShowAnimation = true,", block);
            StringAssert.Contains("HasHideAnimation = false,", block);
        }

        [Test]
        public void The_file_carries_the_lines_the_signal_and_command_generators_insert_after()
        {
            string rendered = Render();

            StringAssert.Contains("public override void SignalBindings()", rendered);
            StringAssert.Contains("base.SignalBindings();", rendered);
            StringAssert.Contains("public override void CommandBindings()", rendered);
            StringAssert.Contains("base.CommandBindings();", rendered);
        }

        [Test]
        public void A_quote_in_a_key_is_escaped()
        {
            _settings.AddressableKey = "Odd\"Name";

            StringAssert.Contains("ScreenLoadCVO.Addressable(\"Odd\\\"Name\")", Render());
        }
    }
}
