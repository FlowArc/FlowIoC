using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PrivateModulePageAdapterTests
    {
        private string _root;
        private string _projectRoot;
        private string _packageRoot;

        private class AdsPage : PrivateModulePage
        {
            public override string Title => "Ads";

            public override string Subtitle => "LevelPlay mediation";

            public override string Icon => "Prefab Icon";

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

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCPrivateAdapter_" + Path.GetRandomFileName());
            _projectRoot = Path.Combine(_root, "project");
            _packageRoot = Path.Combine(_root, "package");

            Directory.CreateDirectory(Path.Combine(_projectRoot, "Assets"));

            string module = Path.Combine(_packageRoot, PrivateModulePayload.Folder, "AdsModule");
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

        private PrivateModulePageAdapter Adapt(PrivateModulePage page) =>
            new PrivateModulePageAdapter(page, _projectRoot, new PrivateModulePayload(_packageRoot));

        [Test]
        public void The_adapter_reads_as_the_page_it_wraps()
        {
            PrivateModulePageAdapter adapter = Adapt(new AdsPage());

            Assert.AreEqual("Ads", adapter.Title);
            Assert.AreEqual("LevelPlay mediation", adapter.Subtitle);
            Assert.AreEqual("Prefab Icon", adapter.Icon);
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
            var adapter = new PrivateModulePageAdapter(
                new AdsPage(), _projectRoot, new PrivateModulePayload((string) null));

            Assert.AreEqual("Unavailable", adapter.Action.Label);
            Assert.IsFalse(adapter.Action.Enabled);
        }
    }
}
