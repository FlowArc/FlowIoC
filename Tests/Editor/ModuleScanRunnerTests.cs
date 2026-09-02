using System;
using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleScanRunnerTests
    {
        private class FakeModuleCheck : IModuleCheck
        {
            private readonly FindingEVO _finding;
            internal int FixCalls;

            internal FakeModuleCheck(string id, ModuleCheckStatus status)
            {
                Id = id;
                _finding = new FindingEVO(id, status, id + " says " + status);
            }

            public string Id { get; }
            public FindingEVO Inspect(ModuleTargetEVO module) => _finding;
            public void Fix(ModuleTargetEVO module) => FixCalls++;
        }

        private class FakeProjectCheck : IProjectCheck
        {
            private readonly FindingEVO _finding;

            internal FakeProjectCheck(string id, ModuleCheckStatus status)
            {
                Id = id;
                _finding = new FindingEVO(id, status, id);
            }

            public string Id { get; }
            public FindingEVO Inspect(ProjectTargetEVO project) => _finding;
            public void Fix(ProjectTargetEVO project) { }
        }

        private class ThrowingCheck : IModuleCheck
        {
            public string Id => "throwing";
            public FindingEVO Inspect(ModuleTargetEVO module) => throw new InvalidOperationException("boom");
            public void Fix(ModuleTargetEVO module) { }
        }

        private static ModuleTargetEVO Target(string name) => new ModuleTargetEVO {Name = name};

        [Test]
        public void Every_module_gets_a_row_carrying_one_finding_per_check()
        {
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new FakeModuleCheck("a", ModuleCheckStatus.Ok),
                    new FakeModuleCheck("b", ModuleCheckStatus.Ok)
                },
                new IProjectCheck[0]);

            ModuleScanReportEVO report = new ModuleScanRunner(pipeline)
                .Run(new ProjectTargetEVO(), new[] {Target("PlayerModule"), Target("InputModule")});

            Assert.AreEqual(2, report.Modules.Count);
            Assert.AreEqual(2, report.Modules[0].Findings.Count);
        }

        [Test]
        public void A_row_is_as_bad_as_its_worst_finding()
        {
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new FakeModuleCheck("a", ModuleCheckStatus.Ok),
                    new FakeModuleCheck("b", ModuleCheckStatus.Manual),
                    new FakeModuleCheck("c", ModuleCheckStatus.Fixable)
                },
                new IProjectCheck[0]);

            ModuleScanReportEVO report = new ModuleScanRunner(pipeline)
                .Run(new ProjectTargetEVO(), new[] {Target("PlayerModule")});

            Assert.AreEqual(ModuleCheckStatus.Manual, report.Modules[0].Status);
        }

        /// <summary>
        /// Scanning is what the window does on focus, so it has to be free of side effects. A
        /// runner that repaired anything would make a rescan destructive.
        /// </summary>
        [Test]
        public void Scanning_never_calls_Fix()
        {
            var check = new FakeModuleCheck("a", ModuleCheckStatus.Fixable);
            var pipeline = new ModuleCheckPipeline(new IModuleCheck[] {check}, new IProjectCheck[0]);

            new ModuleScanRunner(pipeline).Run(new ProjectTargetEVO(), new[] {Target("PlayerModule")});

            Assert.AreEqual(0, check.FixCalls);
        }

        [Test]
        public void Project_checks_land_in_the_project_list_and_count_towards_the_issues()
        {
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[0],
                new IProjectCheck[]
                {
                    new FakeProjectCheck("index", ModuleCheckStatus.Ok),
                    new FakeProjectCheck("orphans", ModuleCheckStatus.Fixable)
                });

            ModuleScanReportEVO report = new ModuleScanRunner(pipeline)
                .Run(new ProjectTargetEVO(), new ModuleTargetEVO[0]);

            Assert.AreEqual(2, report.Project.Count);
            Assert.AreEqual(1, report.IssueCount);
        }

        /// <summary>
        /// A check that throws while inspecting must not cost the report. It becomes the finding
        /// it failed to produce, so the panel shows the failure instead of showing nothing.
        /// </summary>
        [Test]
        public void A_check_that_throws_becomes_a_Manual_finding()
        {
            var pipeline = new ModuleCheckPipeline(new IModuleCheck[] {new ThrowingCheck()}, new IProjectCheck[0]);

            ModuleScanReportEVO report = new ModuleScanRunner(pipeline)
                .Run(new ProjectTargetEVO(), new[] {Target("PlayerModule")});

            Assert.AreEqual(ModuleCheckStatus.Manual, report.Modules[0].Findings[0].Status);
            StringAssert.Contains("boom", report.Modules[0].Findings[0].Message);
        }

        /// <summary>
        /// The row carries what the panel draws in its collapsed state, so the runner has to
        /// copy it off the target rather than leave the window to look the module up again.
        /// </summary>
        [Test]
        public void A_row_carries_the_modules_identity()
        {
            var pipeline = new ModuleCheckPipeline(new IModuleCheck[0], new IProjectCheck[0]);

            var target = new ModuleTargetEVO
            {
                Name = "PlayerModule",
                AssetPath = "Assets/Modules/PlayerModule",
                ExpectedAssemblyName = "Modules.Player"
            };

            ModuleScanReportEVO report = new ModuleScanRunner(pipeline).Run(new ProjectTargetEVO(), new[] {target});

            Assert.AreEqual("PlayerModule", report.Modules[0].Name);
            Assert.AreEqual("Assets/Modules/PlayerModule", report.Modules[0].AssetPath);
            Assert.AreEqual("Modules.Player", report.Modules[0].AssemblyName);
        }
    }
}
