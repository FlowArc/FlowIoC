using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Every script in a test module is wrapped in UNITY_EDITOR - that is the price a test
    /// module pays for being allowed to reference anything. The generators that write into a
    /// module are what has to pay it, so a command or a model written into a test module comes
    /// out wrapped, and one written into a module that ships does not.
    /// </summary>
    public class TestModuleScriptWrapTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "FlowIoC-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private string[] Lines(string fileName) =>
            File.ReadAllLines(Path.Combine(_folder, fileName));

        private static void AssertWrapped(string[] lines)
        {
            Assert.AreEqual("#if UNITY_EDITOR", lines[0].TrimEnd());
            Assert.AreEqual("#endif", lines[lines.Length - 1].TrimEnd());
        }

        private static void AssertNotWrapped(string[] lines)
        {
            Assert.That(string.Join("\n", lines), Does.Not.Contain("UNITY_EDITOR"));
        }

        [Test]
        public void A_command_for_a_test_module_is_wrapped()
        {
            CodeGeneratorUtils.CreateCommand(
                "ProbeCommand", "TempCommand", _folder, CodeGeneratorStrings.TempCommandPath,
                "Modules.Probe.Controllers", new List<string>(), isTest: true);

            AssertWrapped(Lines("ProbeCommand.cs"));
        }

        [Test]
        public void A_command_for_a_module_that_ships_is_not_wrapped()
        {
            CodeGeneratorUtils.CreateCommand(
                "ProbeCommand", "TempCommand", _folder, CodeGeneratorStrings.TempCommandPath,
                "Modules.Probe.Controllers", new List<string>(), isTest: false);

            AssertNotWrapped(Lines("ProbeCommand.cs"));
        }

        [Test]
        public void A_model_and_its_interface_for_a_test_module_are_wrapped()
        {
            CodeGeneratorUtils.CreateModel(
                "ProbeModel", "TempModel", _folder, CodeGeneratorStrings.TempModelPath,
                "Modules.Probe.Models", new List<string>(), isDummy: false, isTest: true);
            CodeGeneratorUtils.CreateModelInterface(
                "IProbeModel", "ITempModel", _folder, CodeGeneratorStrings.TempIModelPath,
                "Modules.Probe.Models", isTest: true);

            AssertWrapped(Lines("ProbeModel.cs"));
            AssertWrapped(Lines("IProbeModel.cs"));
        }

        /// <summary>
        /// A dummy model was always wrapped: it exists for the Editor and never ships. That stays
        /// true whichever module it is written into.
        /// </summary>
        [Test]
        public void A_dummy_model_is_wrapped_whichever_module_it_is_written_into()
        {
            CodeGeneratorUtils.CreateModel(
                "ProbeDummyModel", "TempModel", _folder, CodeGeneratorStrings.TempModelPath,
                "Modules.Probe.Models", new List<string>(), isDummy: true, isTest: false);

            AssertWrapped(Lines("ProbeDummyModel.cs"));
        }

        [Test]
        public void A_model_for_a_module_that_ships_is_not_wrapped()
        {
            CodeGeneratorUtils.CreateModel(
                "ProbeModel", "TempModel", _folder, CodeGeneratorStrings.TempModelPath,
                "Modules.Probe.Models", new List<string>(), isDummy: false, isTest: false);
            CodeGeneratorUtils.CreateModelInterface(
                "IProbeModel", "ITempModel", _folder, CodeGeneratorStrings.TempIModelPath,
                "Modules.Probe.Models", isTest: false);

            AssertNotWrapped(Lines("ProbeModel.cs"));
            AssertNotWrapped(Lines("IProbeModel.cs"));
        }
    }
}
