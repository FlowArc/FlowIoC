using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleRepairTests
    {
        private class RecordingCheck : IModuleCheck
        {
            private readonly ModuleCheckStatus _status;
            private readonly List<string> _log;

            internal RecordingCheck(string id, ModuleCheckStatus status, List<string> log)
            {
                Id = id;
                _status = status;
                _log = log;
            }

            public string Id { get; }
            public FindingEVO Inspect(ModuleTargetEVO module) => new FindingEVO(Id, _status, Id);
            public void Fix(ModuleTargetEVO module) => _log.Add(Id + ":" + module.Name);
        }

        private class ThrowingFix : IModuleCheck
        {
            public string Id => "throwing";
            public FindingEVO Inspect(ModuleTargetEVO module) => FindingEVO.Fixable(Id, "needs work");
            public void Fix(ModuleTargetEVO module) => throw new IOException("locked");
        }

        private class RecordingProjectCheck : IProjectCheck
        {
            private readonly ModuleCheckStatus _status;
            private readonly List<string> _log;

            internal RecordingProjectCheck(string id, ModuleCheckStatus status, List<string> log)
            {
                Id = id;
                _status = status;
                _log = log;
            }

            public string Id { get; }
            public FindingEVO Inspect(ProjectTargetEVO project) => new FindingEVO(Id, _status, Id);
            public void Fix(ProjectTargetEVO project) => _log.Add(Id);
        }

        private static List<ModuleTargetEVO> OneModule() =>
            new List<ModuleTargetEVO> {new ModuleTargetEVO {Name = "PlayerModule"}};

        private static RepairResultEVO Run(ModuleCheckPipeline pipeline, List<ModuleTargetEVO> modules)
        {
            var project = new ProjectTargetEVO();
            ModuleScannerReportEVO report = new ModuleScannerRunner(pipeline).Run(project, modules);

            return new ModuleRepair(pipeline).Apply(report, project, modules);
        }

        [Test]
        public void Only_Fixable_findings_are_applied()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new RecordingCheck("ok", ModuleCheckStatus.Ok, log),
                    new RecordingCheck("fixable", ModuleCheckStatus.Fixable, log),
                    new RecordingCheck("manual", ModuleCheckStatus.Manual, log)
                },
                new IProjectCheck[0]);

            Run(pipeline, OneModule());

            CollectionAssert.AreEqual(new[] {"fixable:PlayerModule"}, log);
        }

        /// <summary>
        /// The repairs depend on each other - see ModuleCheckPipeline - so they run in pipeline
        /// order rather than in the order the findings happen to sit in the report.
        /// </summary>
        [Test]
        public void Fixes_run_in_pipeline_order()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new RecordingCheck("first", ModuleCheckStatus.Fixable, log),
                    new RecordingCheck("second", ModuleCheckStatus.Fixable, log),
                    new RecordingCheck("third", ModuleCheckStatus.Fixable, log)
                },
                new IProjectCheck[0]);

            Run(pipeline, OneModule());

            CollectionAssert.AreEqual(
                new[] {"first:PlayerModule", "second:PlayerModule", "third:PlayerModule"},
                log);
        }

        /// <summary>
        /// A check finishes across every module before the next one starts, because a later
        /// check may read what an earlier one wrote for a different module - the parent Shared
        /// assembly a nested module references, for instance.
        /// </summary>
        [Test]
        public void A_check_runs_over_every_module_before_the_next_check_starts()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new RecordingCheck("first", ModuleCheckStatus.Fixable, log),
                    new RecordingCheck("second", ModuleCheckStatus.Fixable, log)
                },
                new IProjectCheck[0]);

            var modules = new List<ModuleTargetEVO>
            {
                new ModuleTargetEVO {Name = "PlayerModule"},
                new ModuleTargetEVO {Name = "InputModule"}
            };

            Run(pipeline, modules);

            CollectionAssert.AreEqual(
                new[] {"first:PlayerModule", "first:InputModule", "second:PlayerModule", "second:InputModule"},
                log);
        }

        [Test]
        public void Project_checks_run_before_the_module_checks()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[] {new RecordingCheck("module", ModuleCheckStatus.Fixable, log)},
                new IProjectCheck[] {new RecordingProjectCheck("project", ModuleCheckStatus.Fixable, log)});

            Run(pipeline, OneModule());

            CollectionAssert.AreEqual(new[] {"project", "module:PlayerModule"}, log);
        }

        [Test]
        public void A_Fix_that_throws_is_reported_and_the_pipeline_carries_on()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[]
                {
                    new ThrowingFix(),
                    new RecordingCheck("after", ModuleCheckStatus.Fixable, log)
                },
                new IProjectCheck[0]);

            RepairResultEVO result = Run(pipeline, OneModule());

            CollectionAssert.AreEqual(new[] {"after:PlayerModule"}, log);
            Assert.AreEqual(1, result.Fixed);
            Assert.AreEqual(1, result.Remaining.Count);
            StringAssert.Contains("locked", result.Remaining[0]);
        }

        [Test]
        public void Manual_findings_are_listed_as_remaining()
        {
            var log = new List<string>();
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[] {new RecordingCheck("manual", ModuleCheckStatus.Manual, log)},
                new IProjectCheck[0]);

            RepairResultEVO result = Run(pipeline, OneModule());

            Assert.AreEqual(0, result.Fixed);
            Assert.AreEqual(1, result.Remaining.Count);
            StringAssert.Contains("PlayerModule", result.Remaining[0]);
        }
    }
}
