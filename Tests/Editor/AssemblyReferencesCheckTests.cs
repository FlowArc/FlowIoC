using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AssemblyReferencesCheckTests
    {
        private const string ASMDEF = "C:/proj/Assets/Modules/PlayerModule/Modules.Player.asmdef";

        private const string WithEverything = @"{
  ""name"": ""Modules.Player"",
  ""references"": [
    ""FlowIoC"",
    ""Modules.Player.Shared"",
    ""Modules.Game.Shared""
  ]
}";

        private const string WithFlowIoCOnly = @"{
  ""name"": ""Modules.Player"",
  ""references"": [
    ""FlowIoC""
  ]
}";

        private const string WithAHandAddedReference = @"{
  ""name"": ""Modules.Player"",
  ""references"": [
    ""FlowIoC"",
    ""Unity.InputSystem""
  ]
}";

        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main) => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            Kind = kind,
            AbsolutePath = "C:/proj/Assets/Modules/PlayerModule",
            ExpectedAssemblyName = "Modules.Player",
            ParentSharedAssemblyName = "Modules.Game.Shared",
            ParentAssemblyName = "Modules.Game"
        };

        private static AssemblyReferencesCheck Check(
            string content,
            Dictionary<string, string> written = null,
            string asmdefPath = ASMDEF,
            string sharedAssembly = "Modules.Player.Shared")
        {
            return new AssemblyReferencesCheck(
                path => content,
                (path, text) =>
                {
                    if (written != null) written[path] = text;
                },
                module => asmdefPath,
                module => sharedAssembly);
        }

        [Test]
        public void An_asmdef_carrying_every_required_reference_is_Ok()
        {
            Assert.AreEqual(ModuleCheckStatus.Ok, Check(WithEverything).Inspect(Target()).Status);
        }

        [Test]
        public void A_missing_required_reference_is_Fixable_and_named()
        {
            FindingEVO finding = Check(WithFlowIoCOnly).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player.Shared", finding.Message);
            StringAssert.Contains("Modules.Game.Shared", finding.Message);
        }

        /// <summary>
        /// A module that publishes nothing has no Shared assembly to reference, and a top level
        /// module has no parent, so requiring either would leave a permanent warning.
        /// </summary>
        [Test]
        public void A_module_owing_no_references_is_Ok()
        {
            ModuleTargetEVO target = Target();
            target.ParentSharedAssemblyName = null;

            Assert.AreEqual(
                ModuleCheckStatus.Ok,
                Check(WithFlowIoCOnly, sharedAssembly: null).Inspect(target).Status);
        }

        /// <summary>
        /// Whether the assembly exists at all is AssemblyDefinitionCheck's finding to report.
        /// Two checks reporting the same gap would show the module twice as broken.
        /// </summary>
        [Test]
        public void A_module_with_no_asmdef_at_all_is_Ok_here()
        {
            Assert.AreEqual(ModuleCheckStatus.Ok, Check(null, asmdefPath: null).Inspect(Target()).Status);
        }

        /// <summary>
        /// A test module is allowed to reach the module it sits under outright, so its own
        /// assembly reference is required rather than optional.
        /// </summary>
        [Test]
        public void A_test_module_owes_a_reference_to_its_parent_assembly()
        {
            FindingEVO finding = Check(WithFlowIoCOnly, sharedAssembly: null).Inspect(Target(ModuleKind.Test));

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Game", finding.Message);
        }

        /// <summary>
        /// Fix only ever adds. An asmdef may carry references a person put there - a Unity
        /// package, a Service module - and a repair that rewrote the list would silently drop
        /// them, which is the whole reason AssemblyDefinitionReferences exists.
        /// </summary>
        [Test]
        public void Fix_adds_the_missing_references_and_keeps_a_hand_added_one()
        {
            var written = new Dictionary<string, string>();

            Check(WithAHandAddedReference, written).Fix(Target());

            string content = written[ASMDEF];
            StringAssert.Contains("\"Unity.InputSystem\"", content);
            StringAssert.Contains("\"Modules.Player.Shared\"", content);
            StringAssert.Contains("\"Modules.Game.Shared\"", content);
        }
    }
}
