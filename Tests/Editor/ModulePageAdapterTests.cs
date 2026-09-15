using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModulePageAdapterTests
    {
        private string _root;
        private string _projectRoot;
        private string _packageRoot;

        private class AdsPage : ModulePage
        {
            public override string Title => "Ads";

            public override string Subtitle => "LevelPlay mediation";

            public override FlowIcon Icon => FlowIcon.Camera;

            public override string ModuleFolderName => "AdsModule";

            public override IReadOnlyList<HelpTab> MoreTabs =>
                new[] {new HelpTab("Usage", painter => { })};

            public override void DrawBody(HelpPainter painter)
            {
            }
        }

        private class OdinPage : AdsPage
        {
            public override IReadOnlyList<string> RequiredAssemblies =>
                new[] {"Nothing.By.This.Name"};
        }

        private class HintedPage : AdsPage
        {
            public override string BodyHeadline => "Ads in one call.";

            public override string InstalledHint => "Drop AdsRoot into your scene.";
        }

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCModuleAdapter_" + Path.GetRandomFileName());
            _projectRoot = Path.Combine(_root, "project");
            _packageRoot = Path.Combine(_root, "package");

            Directory.CreateDirectory(Path.Combine(_projectRoot, "Assets"));

            string module = Path.Combine(_packageRoot, ModulesSource.ModulesFolder, "AdsModule");
            Directory.CreateDirectory(module);
            File.WriteAllText(
                Path.Combine(module, "Modules.Ads.asmdef"), "{\"name\":\"Modules.Ads\"}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        private ModulePageAdapter Adapt(ModulePage page) =>
            new ModulePageAdapter(page, _projectRoot, new ModulePayload(_packageRoot));

        [Test]
        public void The_adapter_reads_as_the_page_it_wraps()
        {
            ModulePageAdapter adapter = Adapt(new AdsPage());

            Assert.AreEqual("Ads", adapter.Title);
            Assert.AreEqual("LevelPlay mediation", adapter.Subtitle);
            Assert.AreEqual(FlowIcon.Camera, adapter.Icon);
        }

        /// <summary>
        /// The body is a reading of its own and the page's extra tabs follow it, which is how
        /// every module page in the package is already put together.
        /// </summary>
        [Test]
        public void The_body_comes_first_and_the_pages_own_tabs_follow()
        {
            IReadOnlyList<HelpTab> tabs = Adapt(new AdsPage()).Tabs;

            Assert.AreEqual(2, tabs.Count);
            Assert.AreEqual("Introduction", tabs[0].Title);
            Assert.AreEqual("Usage", tabs[1].Title);
        }

        [Test]
        public void A_module_the_project_does_not_have_offers_to_install()
        {
            HelpAction action = Adapt(new AdsPage()).Action;

            Assert.AreEqual("Install", action.Label);
            Assert.IsTrue(action.Enabled);
        }

        [Test]
        public void A_module_whose_assembly_is_absent_offers_nothing()
        {
            HelpAction action = Adapt(new OdinPage()).Action;

            Assert.AreEqual("Missing", action.Label);
            Assert.IsFalse(action.Enabled);
        }

        /// <summary>
        /// Installed is decided by the assembly the shipped module declares, not by the folder
        /// name, because once installed the folder belongs to the game and may be renamed. That
        /// rule lives in ModuleInstaller; this is the adapter asking it the right question.
        /// </summary>
        [Test]
        public void A_module_already_in_the_project_is_reported_as_installed()
        {
            string installed = Path.Combine(_projectRoot, "Assets", "Modules", "Renamed");
            Directory.CreateDirectory(installed);
            File.WriteAllText(
                Path.Combine(installed, "Modules.Ads.asmdef"), "{\"name\":\"Modules.Ads\"}");

            HelpAction action = Adapt(new AdsPage()).Action;

            Assert.AreEqual("Installed", action.Label);
            Assert.IsFalse(action.Enabled);
        }

        [Test]
        public void A_page_with_no_package_behind_it_offers_nothing()
        {
            var adapter = new ModulePageAdapter(
                new AdsPage(), _projectRoot, new ModulePayload((string) null));

            Assert.AreEqual("Unavailable", adapter.Action.Label);
            Assert.IsFalse(adapter.Action.Enabled);
        }

        /// <summary>
        /// The band under the banner reads what the page says there, the way it does for the
        /// pages FlowIoC writes itself.
        /// </summary>
        [Test]
        public void The_adapter_carries_the_pages_headline()
        {
            IReadOnlyList<HelpTab> tabs = Adapt(new HintedPage()).Tabs;

            Assert.AreEqual("Ads in one call.", tabs[0].Headline);
        }

        /// <summary>
        /// The dialog that reports an install says where the module landed and then whatever
        /// the page adds - where to drop the Root, which tab has the steps.
        /// </summary>
        [Test]
        public void The_installed_message_carries_the_pages_hint()
        {
            string message = Adapt(new HintedPage()).InstalledMessage();

            StringAssert.Contains("Assets/Modules/AdsModule", message);
            StringAssert.EndsWith("installs are made from. Drop AdsRoot into your scene.", message);
        }

        [Test]
        public void The_installed_message_ends_with_the_generic_sentence_without_a_hint()
        {
            StringAssert.EndsWith("installs are made from.", Adapt(new AdsPage()).InstalledMessage());
        }
    }
}