using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SharedAssemblyCheckTests
    {
        private const string SHARED = "C:/proj/Assets/Modules/PlayerModule/Scripts/Shared";

        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main) => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            Kind = kind,
            AbsolutePath = "C:/proj/Assets/Modules/PlayerModule",
            ExpectedAssemblyName = "Modules.Player"
        };

        [Test]
        public void A_Shared_folder_holding_its_asmdef_is_Ok()
        {
            var check = new SharedAssemblyCheck(
                module => SHARED,
                folder => new[] {SHARED + "/Modules.Player.Shared.asmdef"},
                module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        [Test]
        public void A_Shared_folder_with_no_asmdef_is_Fixable_and_names_the_assembly_it_wants()
        {
            var check = new SharedAssemblyCheck(module => SHARED, folder => new string[0], module => { });

            FindingEVO finding = check.Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player.Shared", finding.Message);
        }

        /// <summary>
        /// A module with no Shared folder publishes nothing, which is the ordinary case. It is
        /// not a problem and must not show as one.
        /// </summary>
        [Test]
        public void A_module_with_no_Shared_folder_is_Ok()
        {
            var check = new SharedAssemblyCheck(module => null, folder => new string[0], module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        /// <summary>
        /// A test module may reference anything directly and so publishes nothing through a
        /// Shared assembly. Checking one would keep it permanently yellow.
        /// </summary>
        [Test]
        public void A_test_module_is_skipped()
        {
            var check = new SharedAssemblyCheck(module => SHARED, folder => new string[0], module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target(ModuleKind.Test)).Status);
        }

        [Test]
        public void Fix_creates_the_Shared_assembly()
        {
            bool created = false;
            var check = new SharedAssemblyCheck(module => SHARED, folder => new string[0], module => created = true);

            check.Fix(Target());

            Assert.IsTrue(created);
        }
    }
}
