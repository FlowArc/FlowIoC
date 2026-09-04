using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LogTypeCheckTests
    {
        private static ProjectTargetEVO Project(
            IReadOnlyList<string> registered,
            params (string Name, ModuleKind Kind)[] modules)
        {
            var scanned = new List<ScannedModule>();

            foreach ((string name, ModuleKind kind) in modules)
                scanned.Add(new ScannedModule {Name = name, Kind = kind, AbsolutePath = name});

            return new ProjectTargetEVO {RegisteredAutoLogTypes = registered, ScannedModules = scanned};
        }

        [Test]
        public void A_log_type_for_every_module_is_Ok()
        {
            var check = new LogTypeCheck(names => { }, names => { });

            Assert.AreEqual(
                ModuleCheckStatus.Ok,
                check.Inspect(Project(new[] {"PlayerModule"}, ("PlayerModule", ModuleKind.Main))).Status);
        }

        [Test]
        public void A_module_with_no_log_type_is_Fixable_and_named()
        {
            var check = new LogTypeCheck(names => { }, names => { });

            FindingEVO finding = check.Inspect(Project(new string[0], ("PlayerModule", ModuleKind.Main)));

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("PlayerModule", finding.Message);
        }

        /// <summary>
        /// A test module runs only in the editor and gets no channel of its own, so requiring one
        /// would keep the check permanently yellow.
        /// </summary>
        [Test]
        public void A_test_module_is_not_expected_to_have_one()
        {
            var check = new LogTypeCheck(names => { }, names => { });

            Assert.AreEqual(
                ModuleCheckStatus.Ok,
                check.Inspect(Project(new string[0], ("PlayerTestModule", ModuleKind.Test))).Status);
        }

        [Test]
        public void A_log_type_whose_module_is_gone_is_Fixable()
        {
            var check = new LogTypeCheck(names => { }, names => { });

            FindingEVO finding = check.Inspect(
                Project(new[] {"PlayerModule", "OldModule"}, ("PlayerModule", ModuleKind.Main)));

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("OldModule", finding.Message);
        }

        [Test]
        public void Fix_adds_the_missing_types_and_removes_the_dead_ones()
        {
            List<string> added = null;
            List<string> removed = null;
            var check = new LogTypeCheck(names => added = names, names => removed = names);

            check.Fix(Project(new[] {"OldModule"}, ("PlayerModule", ModuleKind.Main)));

            CollectionAssert.AreEqual(new[] {"PlayerModule"}, added);
            CollectionAssert.AreEqual(new[] {"OldModule"}, removed);
        }

        /// <summary>
        /// ModuleLogTypePlan refuses to propose removals from an empty module list, because a
        /// scan that found nothing is a failed scan rather than an empty project. The check
        /// inherits that and must not report a project as broken on the strength of it.
        /// </summary>
        [Test]
        public void An_empty_scan_proposes_no_removals()
        {
            var check = new LogTypeCheck(names => { }, names => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Project(new[] {"PlayerModule"})).Status);
        }
    }
}
