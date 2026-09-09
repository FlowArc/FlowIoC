using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A module's Signals assembly has to see the module's own Shared assembly, because a public
    /// signal is often generic over a type the module publishes and the two are separate
    /// assemblies.
    ///
    /// Create Module writes that reference when both are made together and SignalsInstaller writes
    /// it when Signals arrives after Shared. The order that was missed is Shared after Signals:
    /// Add Shared wired the module's own asmdef and its children and left the Signals assembly
    /// naming nothing, so the module looked finished and the first such signal failed with CS0012.
    /// </summary>
    public class SignalsSharedReferenceCheckTests
    {
        private const string SIGNALS_ASMDEF =
            "C:/Project/Assets/Modules/PlayerModule/Scripts/Signals/Modules.Player.Signals.asmdef";

        private static string Referencing(params string[] names) =>
            "{\"name\": \"Modules.Player.Signals\", \"references\": [\"" + string.Join("\", \"", names) + "\"]}";

        private static ModuleTargetEVO Module(ModuleKind kind = ModuleKind.Main) =>
            new ModuleTargetEVO {Name = "PlayerModule", Kind = kind};

        private static SignalsSharedReferenceCheck Checking(
            string asmdefPath, string shared, IDictionary<string, string> files) =>
            new SignalsSharedReferenceCheck(
                _ => asmdefPath,
                _ => shared,
                path => path != null && files.TryGetValue(path, out string content) ? content : null,
                (path, content) => files[path] = content);

        [Test]
        public void A_Signals_assembly_that_names_Shared_is_Ok()
        {
            var files = new Dictionary<string, string>
                {{SIGNALS_ASMDEF, Referencing("FlowIoC", "Modules.Player.Shared")}};

            FindingEVO finding = Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files).Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        [Test]
        public void A_Signals_assembly_that_does_not_name_Shared_is_fixable()
        {
            var files = new Dictionary<string, string> {{SIGNALS_ASMDEF, Referencing("FlowIoC")}};

            FindingEVO finding = Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files).Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player.Shared", finding.Message);
        }

        [Test]
        public void Fix_adds_the_reference()
        {
            var files = new Dictionary<string, string> {{SIGNALS_ASMDEF, Referencing("FlowIoC")}};
            SignalsSharedReferenceCheck check = Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files);

            check.Fix(Module());

            StringAssert.Contains("\"Modules.Player.Shared\"", files[SIGNALS_ASMDEF]);
            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Module()).Status);
        }

        /// <summary>
        /// Fix only ever adds. A holder generic over another module's published type needs that
        /// module's Shared as well, added by hand, and rewriting the list would drop it.
        /// </summary>
        [Test]
        public void Fix_keeps_a_reference_somebody_added_by_hand()
        {
            var files = new Dictionary<string, string>
                {{SIGNALS_ASMDEF, Referencing("FlowIoC", "Modules.Gameplay.Shared")}};

            Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files).Fix(Module());

            StringAssert.Contains("\"Modules.Gameplay.Shared\"", files[SIGNALS_ASMDEF]);
            StringAssert.Contains("\"Modules.Player.Shared\"", files[SIGNALS_ASMDEF]);
        }

        /// <summary>
        /// A module that publishes nothing has nothing for its holder to be generic over, so there
        /// is no reference to want.
        /// </summary>
        [Test]
        public void A_module_with_no_Shared_assembly_is_Ok()
        {
            var files = new Dictionary<string, string> {{SIGNALS_ASMDEF, Referencing("FlowIoC")}};

            FindingEVO finding = Checking(SIGNALS_ASMDEF, null, files).Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// Whether the Signals assembly exists at all is SignalsAssemblyCheck's finding. Reporting
        /// it here as well would show one gap twice on one module.
        /// </summary>
        [Test]
        public void A_module_with_no_Signals_assembly_is_Ok()
        {
            FindingEVO finding = Checking(null, "Modules.Player.Shared", new Dictionary<string, string>())
                .Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// A test module keeps its holder in Scripts/Runtime/Signals and has no Signals assembly,
        /// so it is never asked.
        /// </summary>
        [Test]
        public void A_test_module_is_Ok()
        {
            var files = new Dictionary<string, string> {{SIGNALS_ASMDEF, Referencing("FlowIoC")}};

            FindingEVO finding =
                Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files).Inspect(Module(ModuleKind.Test));

            Assert.AreEqual(ModuleCheckStatus.Ok, finding.Status);
        }

        /// <summary>
        /// The name is matched quoted, so one assembly's name cannot be found inside another's -
        /// "Modules.Player.Shared" does not appear inside a list that only names "Modules.Player".
        /// </summary>
        [Test]
        public void A_reference_to_the_module_itself_does_not_count_as_Shared()
        {
            var files = new Dictionary<string, string>
                {{SIGNALS_ASMDEF, Referencing("FlowIoC", "Modules.Player")}};

            FindingEVO finding = Checking(SIGNALS_ASMDEF, "Modules.Player.Shared", files).Inspect(Module());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
        }
    }
}
