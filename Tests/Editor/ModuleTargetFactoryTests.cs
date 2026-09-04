using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The factory is the one part of the scan that talks to Unity, so it is the one part a
    /// fixture cannot describe. These run it against whatever project the tests are executing in
    /// and assert the shape of what comes back rather than its contents - the checks themselves
    /// are covered by their own tests.
    /// </summary>
    public class ModuleTargetFactoryTests
    {
        private ProjectTargetEVO _project;
        private List<ModuleTargetEVO> _modules;

        [SetUp]
        public void SetUp()
        {
            (_project, _modules) = new ModuleTargetFactory().Build();
        }

        [Test]
        public void Every_scanned_module_becomes_a_target()
        {
            Assert.AreEqual(_project.ScannedModules.Count, _modules.Count);
        }

        [Test]
        public void Every_target_carries_the_identity_a_check_needs()
        {
            foreach (ModuleTargetEVO module in _modules)
            {
                Assert.IsNotEmpty(module.Name, "a target with no name");
                Assert.IsNotEmpty(module.AbsolutePath, $"{module.Name} has no path");
                Assert.IsNotEmpty(module.ExpectedAssemblyName, $"{module.Name} resolves to no assembly name");
                Assert.IsNotEmpty(module.ProjectRoot, $"{module.Name} has no project root");
            }
        }

        /// <summary>
        /// A nested module has to find the module it lives in, because that is where its parent
        /// Shared assembly reference comes from. A module directly under a modules root has none.
        /// </summary>
        [Test]
        public void A_nested_module_finds_the_module_it_lives_in()
        {
            foreach (ModuleTargetEVO module in _modules)
            {
                if (module.ParentAbsolutePath == null) continue;

                StringAssert.StartsWith(
                    module.ParentAbsolutePath.Replace('\\', '/'),
                    module.AbsolutePath.Replace('\\', '/'));

                Assert.AreNotEqual(module.ParentAbsolutePath, module.AbsolutePath);
            }
        }

        [Test]
        public void The_project_target_knows_the_assemblies_and_the_index()
        {
            Assert.IsNotNull(_project.Index, "the module index could not be loaded");
            Assert.Greater(_project.AllAssemblyNames.Count, 0, "no assemblies found in Assets or Packages");
        }

        /// <summary>
        /// A scan must never write. The window rescans on focus, so a scan with a side effect
        /// would fire every time the window is clicked.
        /// </summary>
        [Test]
        public void Running_the_whole_pipeline_over_this_project_reports_without_writing()
        {
            ModuleScannerReportEVO report = new ModuleScannerRunner(new ModuleCheckPipeline()).Run(_project, _modules);

            Assert.AreEqual(_modules.Count, report.Modules.Count);
            Assert.AreEqual(new ModuleCheckPipeline().ProjectChecks.Count, report.Project.Count);

            foreach (ModuleRowEVO row in report.Modules)
            {
                Assert.AreEqual(
                    new ModuleCheckPipeline().ModuleChecks.Count,
                    row.Findings.Count,
                    $"{row.Name} did not get one finding per check");
            }
        }
    }
}
