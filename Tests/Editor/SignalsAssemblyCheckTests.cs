using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalsAssemblyCheckTests
    {
        private const string SIGNALS = "C:/proj/Assets/Modules/PlayerModule/Scripts/Signals";

        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main) => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            Kind = kind,
            AbsolutePath = "C:/proj/Assets/Modules/PlayerModule",
            ExpectedAssemblyName = "Modules.Player"
        };

        [Test]
        public void A_Signals_folder_holding_its_asmdef_is_Ok()
        {
            var check = new SignalsAssemblyCheck(
                module => SIGNALS,
                folder => new[] {SIGNALS + "/Modules.Player.Signals.asmdef"},
                module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        /// <summary>
        /// Without the asmdef the holder compiles into the module's own assembly, so a Connector
        /// could only reach it by referencing the module's Models and Commands - which is the one
        /// thing the split exists to prevent.
        /// </summary>
        [Test]
        public void A_Signals_folder_with_no_asmdef_is_Fixable_and_names_the_assembly_it_wants()
        {
            var check = new SignalsAssemblyCheck(module => SIGNALS, folder => new string[0], module => { });

            FindingEVO finding = check.Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player.Signals", finding.Message);
        }

        /// <summary>
        /// A module whose layout never laid the folder down - or one written before the folder
        /// existed - has nothing to report. MandatoryFoldersCheck is what creates it.
        /// </summary>
        [Test]
        public void A_module_with_no_Signals_folder_is_Ok()
        {
            var check = new SignalsAssemblyCheck(module => null, folder => new string[0], module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        /// <summary>
        /// A test module keeps its holder in Scripts/Runtime/Signals and is wired to directly, so
        /// checking one would keep it permanently yellow.
        /// </summary>
        [Test]
        public void A_test_module_is_skipped()
        {
            var check = new SignalsAssemblyCheck(module => SIGNALS, folder => new string[0], module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target(ModuleKind.Test)).Status);
        }

        /// <summary>
        /// The folder is mandatory, so a module that announces nothing still has it - a Connector
        /// is the honest case, since nothing dispatches into one. Writing an assembly for an empty
        /// folder would ship a DLL with nothing in it.
        /// </summary>
        [Test]
        public void An_empty_Signals_folder_is_Ok()
        {
            var check = new SignalsAssemblyCheck(
                module => SIGNALS,
                folder => new string[0],
                folder => new string[0],
                module => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        [Test]
        public void Fix_creates_the_Signals_assembly()
        {
            bool created = false;
            var check = new SignalsAssemblyCheck(module => SIGNALS, folder => new string[0], module => created = true);

            check.Fix(Target());

            Assert.IsTrue(created);
        }
    }
}