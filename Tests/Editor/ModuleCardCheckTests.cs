using System.Collections.Generic;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardCheckTests
    {
        private string _written;

        [SetUp]
        public void SetUp() => _written = null;

        private static ModuleTargetEVO Module(ModuleKind kind = ModuleKind.Main)
        {
            return new ModuleTargetEVO
            {
                Name = "PlayerModule",
                Kind = kind,
                AbsolutePath = "PlayerModule",
                ExpectedAssemblyName = "Modules.Player",
            };
        }

        private static ModuleFactsEVO Facts()
        {
            return new ModuleFactsEVO
            {
                Kind = "Main",
                Assemblies = new List<string> {"Modules.Player"},
            };
        }

        private ModuleCardCheck Check(string card, ModuleFactsEVO facts)
        {
            _written = null;

            return new ModuleCardCheck(
                module => card,
                (module, text) => _written = text,
                module => facts);
        }

        [Test]
        public void A_test_module_carries_no_card()
        {
            ModuleCardCheck check = Check(null, Facts());

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Module(ModuleKind.Test)).Status);
        }

        [Test]
        public void A_module_with_no_card_is_Fixable()
        {
            ModuleCardCheck check = Check(null, Facts());

            Assert.AreEqual(ModuleCheckStatus.Fixable, check.Inspect(Module()).Status);
        }

        [Test]
        public void Fixing_a_module_with_no_card_writes_the_stub_and_the_block()
        {
            ModuleCardCheck check = Check(null, Facts());

            check.Fix(Module());

            StringAssert.Contains("# PlayerModule", _written);
            StringAssert.Contains(ModuleCardStub.PURPOSE_PLACEHOLDER, _written);
            StringAssert.Contains("<!-- FLOWIOC:BEGIN version=1", _written);
        }

        [Test]
        public void A_card_whose_block_is_stale_is_Fixable()
        {
            string card = new ModuleCardWriter().Write(new ModuleCardStub().For("PlayerModule"), "**Kind** Screen").Text;

            Assert.AreEqual(ModuleCheckStatus.Fixable, Check(card, Facts()).Inspect(Module()).Status);
        }

        [Test]
        public void A_card_that_is_current_but_unfilled_is_Manual()
        {
            string body = new ModuleCardBodyBuilder().Build(Facts());
            string card = new ModuleCardWriter().Write(new ModuleCardStub().For("PlayerModule"), body).Text;

            FindingEVO finding = Check(card, Facts()).Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("purpose", finding.Message.ToLowerInvariant());
        }

        [Test]
        public void A_filled_current_card_is_Ok()
        {
            string body = new ModuleCardBodyBuilder().Build(Facts());
            string authored = "# PlayerModule\n\n## Purpose\nOwns the money.\n\n## Concepts\ncurrency\n";
            string card = new ModuleCardWriter().Write(authored, body).Text;

            Assert.AreEqual(ModuleCheckStatus.Ok, Check(card, Facts()).Inspect(Module()).Status);
        }

        [Test]
        public void A_module_whose_assembly_is_not_loaded_is_Manual()
        {
            ModuleCardCheck check = Check("# PlayerModule\n\n## Purpose\nOwns the money.\n\n## Concepts\nmoney\n", null);

            FindingEVO finding = check.Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("compile", finding.Message.ToLowerInvariant());
        }

        /// <summary>
        /// The panel turns a finding that names an asset into a row that pings it, and lights
        /// that row up under the pointer. A finding with no path is drawn quiet and takes no
        /// click, so what the check puts here decides whether the row is interactive at all.
        /// </summary>
        [Test]
        public void A_card_finding_points_at_the_card()
        {
            ModuleTargetEVO module = Module();
            module.AssetPath = "Assets/Modules/PlayerModule";

            string body = new ModuleCardBodyBuilder().Build(Facts());
            string card = new ModuleCardWriter().Write(new ModuleCardStub().For("PlayerModule"), body).Text;

            Assert.AreEqual("Assets/Modules/PlayerModule/MODULE.md", Check(card, Facts()).Inspect(module).AssetPath);
        }

        [Test]
        public void A_module_with_no_card_points_at_the_module_folder_instead()
        {
            ModuleTargetEVO module = Module();
            module.AssetPath = "Assets/Modules/PlayerModule";

            Assert.AreEqual("Assets/Modules/PlayerModule", Check(null, Facts()).Inspect(module).AssetPath);
        }

        [Test]
        public void A_card_whose_markers_are_broken_is_Manual()
        {
            string broken = new ModuleCardStub().For("PlayerModule") + "\n<!-- FLOWIOC:END -->\n";

            Assert.AreEqual(ModuleCheckStatus.Manual, Check(broken, Facts()).Inspect(Module()).Status);
        }
    }
}