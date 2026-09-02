using System.Collections.Generic;
using FlowIoC.Editor.ModuleScan;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AssemblyDefinitionCheckTests
    {
        private const string MODULE = "C:/proj/Assets/Modules/PlayerModule";

        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main) => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            Kind = kind,
            AbsolutePath = MODULE,
            ExpectedAssemblyName = "Modules.Player",
            ParentSharedAssemblyName = "Modules.Game.Shared",
            ParentAssemblyName = "Modules.Game"
        };

        private static AssemblyDefinitionCheck Check(
            string[] asmdefs,
            Dictionary<string, string> written = null,
            string sharedAssembly = "Modules.Player.Shared")
        {
            return new AssemblyDefinitionCheck(
                folder => asmdefs,
                (path, content) =>
                {
                    if (written != null) written[path.Replace('\\', '/')] = content;
                },
                module => sharedAssembly);
        }

        [Test]
        public void A_module_whose_asmdef_is_correctly_named_is_Ok()
        {
            Assert.AreEqual(
                ModuleCheckStatus.Ok,
                Check(new[] {MODULE + "/Modules.Player.asmdef"}).Inspect(Target()).Status);
        }

        [Test]
        public void A_module_with_no_asmdef_is_Fixable()
        {
            FindingEVO finding = Check(new string[0]).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Player", finding.Message);
        }

        /// <summary>
        /// Renaming an assembly cascades into every asmdef that references it by name and into
        /// the root .csproj.DotSettings named after it, so the panel reports it and stops.
        /// </summary>
        [Test]
        public void A_wrongly_named_asmdef_is_Manual()
        {
            FindingEVO finding = Check(new[] {MODULE + "/PlayerModule.asmdef"}).Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Manual, finding.Status);
            StringAssert.Contains("PlayerModule", finding.Message);
            StringAssert.Contains("Modules.Player", finding.Message);
        }

        [Test]
        public void More_than_one_asmdef_is_Manual()
        {
            string[] asmdefs = {MODULE + "/Modules.Player.asmdef", MODULE + "/Legacy.asmdef"};

            Assert.AreEqual(ModuleCheckStatus.Manual, Check(asmdefs).Inspect(Target()).Status);
        }

        [Test]
        public void Fix_writes_an_asmdef_referencing_FlowIoC_and_both_Shared_assemblies()
        {
            var written = new Dictionary<string, string>();

            Check(new string[0], written).Fix(Target());

            string content = written[MODULE + "/Modules.Player.asmdef"];
            StringAssert.Contains("\"name\": \"Modules.Player\"", content);
            StringAssert.Contains("\"FlowIoC\"", content);
            StringAssert.Contains("\"Modules.Player.Shared\"", content);
            StringAssert.Contains("\"Modules.Game.Shared\"", content);
        }

        /// <summary>
        /// A test module exists to exercise the module it sits under and is allowed to reach it
        /// outright - the one case where a module references a neighbour's own assembly rather
        /// than only the data it publishes.
        /// </summary>
        [Test]
        public void A_test_module_also_references_its_parent_assembly()
        {
            var written = new Dictionary<string, string>();

            ModuleTargetEVO target = Target(ModuleKind.Test);
            target.ExpectedAssemblyName = "Modules.Player.Test";
            target.AbsolutePath = MODULE + "/zTestModules/PlayerTestModule";

            Check(new string[0], written, sharedAssembly: null).Fix(target);

            string content = written[MODULE + "/zTestModules/PlayerTestModule/Modules.Player.Test.asmdef"];
            StringAssert.Contains("\"Modules.Game\"", content);
        }

        /// <summary>
        /// A module that publishes nothing has no Shared assembly, and naming one anyway would
        /// leave the asmdef pointing at an assembly nothing produces - which is exactly what
        /// ModuleGenerator avoids by writing whatever CreateFor actually made.
        /// </summary>
        [Test]
        public void A_module_with_no_Shared_assembly_gets_no_Shared_reference()
        {
            var written = new Dictionary<string, string>();

            Check(new string[0], written, sharedAssembly: null).Fix(Target());

            StringAssert.DoesNotContain("Modules.Player.Shared", written[MODULE + "/Modules.Player.asmdef"]);
        }

        /// <summary>
        /// A top level module has no parent to publish to it, and the template drops the empty
        /// reference rather than writing an entry naming nothing.
        /// </summary>
        [Test]
        public void A_module_with_no_parent_gets_no_parent_reference()
        {
            var written = new Dictionary<string, string>();

            ModuleTargetEVO target = Target();
            target.ParentSharedAssemblyName = null;
            target.ParentAssemblyName = null;

            Check(new string[0], written).Fix(target);

            string content = written[MODULE + "/Modules.Player.asmdef"];
            StringAssert.DoesNotContain("Modules.Game", content);
            StringAssert.Contains("\"Modules.Player.Shared\"", content);
        }
    }
}
