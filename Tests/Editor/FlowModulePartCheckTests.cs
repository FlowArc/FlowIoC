using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A module's Flow Console channel is the generated part on disk, so this watches the part
    /// and the asmref that decides which assembly it lands in.
    /// </summary>
    public class FlowModulePartCheckTests
    {
        private const string MODULE_PATH = "C:/Project/Assets/Modules/PlayerModule";

        private readonly List<string> _present = new List<string>();
        private int _regenerated;

        private FlowModulePartCheck Check()
        {
            return new FlowModulePartCheck(
                path => _present.Contains(path.Replace('\\', '/')),
                () => _regenerated++);
        }

        private static ModuleTargetEVO Module(ModuleKind kind = ModuleKind.Main)
        {
            return new ModuleTargetEVO {Name = "PlayerModule", Kind = kind, AbsolutePath = MODULE_PATH};
        }

        private void Given(params string[] files)
        {
            _present.Clear();

            foreach (string file in files)
                _present.Add(MODULE_PATH + "/Scripts/Generated/" + file);
        }

        [SetUp]
        public void SetUp()
        {
            _present.Clear();
            _regenerated = 0;
        }

        [Test]
        public void A_module_with_both_files_is_ok()
        {
            Given("FlowModule.PlayerModule.cs", "FlowIoC.Generated.asmref");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Module()).Status);
        }

        [Test]
        public void A_missing_part_is_fixable()
        {
            Given("FlowIoC.Generated.asmref");

            FindingEVO finding = Check().Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("FlowModule.PlayerModule.cs", finding.Message);
        }

        /// <summary>
        /// Without the asmref the part still compiles - into the module's own assembly, where it
        /// is a second FlowModule that shares a name with the real one and nothing else. That is
        /// worth reporting on its own rather than only noticing the file is there.
        /// </summary>
        [Test]
        public void A_missing_asmref_is_fixable()
        {
            Given("FlowModule.PlayerModule.cs");

            FindingEVO finding = Check().Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("FlowIoC.Generated.asmref", finding.Message);
        }

        [Test]
        public void A_test_module_owes_no_part()
        {
            Given();

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Module(ModuleKind.Test)).Status);
        }

        [Test]
        public void The_repair_runs_the_generator_rather_than_writing_the_file_itself()
        {
            Given();

            Check().Fix(Module());

            Assert.AreEqual(1, _regenerated);
        }

        [Test]
        public void The_part_and_the_asmref_sit_in_the_modules_own_Generated_folder()
        {
            ModuleTargetEVO module = Module();

            Assert.AreEqual(
                MODULE_PATH + "/Scripts/Generated/FlowModule.PlayerModule.cs",
                FlowModulePartCheck.PartPathOf(module).Replace('\\', '/'));

            Assert.AreEqual(
                MODULE_PATH + "/Scripts/Generated/FlowIoC.Generated.asmref",
                FlowModulePartCheck.AsmRefPathOf(module).Replace('\\', '/'));
        }

        /// <summary>
        /// The check is in the pipeline, because a check nothing runs reports nothing.
        /// </summary>
        [Test]
        public void The_check_is_one_the_scanner_runs()
        {
            var pipeline = new ModuleCheckPipeline();

            bool present = false;
            foreach (IModuleCheck check in pipeline.ModuleChecks)
                present |= check is FlowModulePartCheck;

            Assert.IsTrue(present);
        }
    }
}
