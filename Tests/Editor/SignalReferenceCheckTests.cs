using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalReferenceCheckTests
    {
        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main, string parentSignals = null) =>
            new ModuleTargetEVO
            {
                Name = "PlayerModule",
                Kind = kind,
                AbsolutePath = "C:/proj/Assets/Modules/PlayerModule",
                ExpectedAssemblyName = "Modules.Player",
                ParentSignalsAssemblyName = parentSignals
            };

        private static string Asmdef(params string[] references) =>
            "{\n  \"name\": \"Modules.Player\",\n  \"references\": [\n"
            + string.Join(",\n", System.Array.ConvertAll(references, reference => "    \"" + reference + "\""))
            + "\n  ],\n  \"autoReferenced\": true\n}";

        private static SignalReferenceCheck Check(string asmdef, bool isConnector = false) =>
            new SignalReferenceCheck(module => asmdef, module => "Modules.Player.Signals", module => isConnector);

        [Test]
        public void A_module_naming_only_its_own_Signals_assembly_is_Ok()
        {
            FindingEVO finding = Check(Asmdef("FlowIoC", "Modules.Player.Signals")).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// Referencing a neighbour's Shared assembly to read a published enum is ordinary and
        /// allowed. That is exactly why this check could not be written before the split: against
        /// Shared there was nothing to tell the two intents apart.
        /// </summary>
        [Test]
        public void A_module_reading_another_modules_Shared_data_is_Ok()
        {
            FindingEVO finding = Check(Asmdef("FlowIoC", "Modules.Gameplay.Shared")).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        [Test]
        public void A_module_naming_another_modules_Signals_assembly_is_reported()
        {
            FindingEVO finding = Check(Asmdef("FlowIoC", "Modules.Gameplay.Signals")).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("Modules.Gameplay.Signals", finding.Message);
        }

        /// <summary>
        /// Removing a reference breaks whatever was using it, and what to do instead is a decision
        /// about the game. Fix All must not touch this one.
        /// </summary>
        [Test]
        public void The_finding_is_Manual_rather_than_Fixable()
        {
            FindingEVO finding = Check(Asmdef("Modules.Gameplay.Signals")).Inspect(Target());

            Assert.AreNotEqual(ModuleCheckStatus.Fixable, finding.Status);
        }

        [Test]
        public void Every_foreign_reference_is_named_once()
        {
            FindingEVO finding = Check(
                    Asmdef("FlowIoC", "Modules.Gameplay.Signals", "Modules.Main.Signals", "Modules.Gameplay.Signals"))
                .Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("Modules.Gameplay.Signals", finding.Message);
            StringAssert.Contains("Modules.Main.Signals", finding.Message);
            Assert.AreEqual(
                1, System.Text.RegularExpressions.Regex.Matches(finding.Message, "Modules.Gameplay.Signals").Count);
        }

        /// <summary>
        /// A Connector is the crossing point, so every Signals assembly it names is the job rather
        /// than a breach of it - and its list is the longest in the project.
        /// </summary>
        [Test]
        public void A_Connector_may_name_any_of_them()
        {
            FindingEVO finding = Check(
                    Asmdef("FlowIoC", "Modules.Gameplay.Signals", "Modules.Main.Signals"), isConnector: true)
                .Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// A screen or sub module may use the types of the module it lives in, and a test module
        /// may use anything. The parent's holder is therefore allowed whatever the kind.
        /// </summary>
        [Test]
        public void The_parents_Signals_assembly_is_allowed()
        {
            FindingEVO finding = Check(Asmdef("FlowIoC", "Modules.Hero.Signals"))
                .Inspect(Target(ModuleKind.Screen, "Modules.Hero.Signals"));

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        [Test]
        public void A_test_module_may_name_its_parents_Signals_assembly()
        {
            FindingEVO finding = Check(Asmdef("FlowIoC", "Modules.Hero", "Modules.Hero.Signals"))
                .Inspect(Target(ModuleKind.Test, "Modules.Hero.Signals"));

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// Whether the assembly exists at all is AssemblyDefinitionCheck's finding to make.
        /// Reporting it here as well would show the same gap twice on one module.
        /// </summary>
        [Test]
        public void A_module_with_no_assembly_is_Ok()
        {
            FindingEVO finding = Check(null).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// Only the reference array is read. A module named for what it does could otherwise be
        /// mistaken for a reference to it.
        /// </summary>
        [Test]
        public void Quoted_text_outside_the_reference_array_is_not_a_reference()
        {
            const string asmdef = "{\n  \"name\": \"Modules.Player\",\n  \"references\": [\n    \"FlowIoC\"\n  ],\n"
                                  + "  \"versionDefines\": [\n    \"Modules.Gameplay.Signals\"\n  ]\n}";

            Assert.AreEqual(ModuleCheckStatus.Ok, Check(asmdef).Inspect(Target()).Status);
        }
    }
}
