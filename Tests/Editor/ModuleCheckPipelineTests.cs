using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCheckPipelineTests
    {
        /// <summary>
        /// The repairs depend on each other: Scripts/Shared and Scripts/Signals have to exist
        /// before their asmdefs can be written, the Signals assembly references Shared so Shared
        /// comes first, the module asmdef references both so both come before it, the references
        /// are added to an asmdef that must already exist, and the settings file name derives from
        /// the final assembly name, and the card is written last because its block names the
        /// assemblies every check above it settles. Changing this order changes what Fix All
        /// produces, so it is asserted rather than left to whoever edits the list.
        /// </summary>
        [Test]
        public void Module_checks_run_in_the_order_the_repairs_depend_on()
        {
            List<string> ids = new ModuleCheckPipeline().ModuleChecks.Select(check => check.Id).ToList();

            CollectionAssert.AreEqual(
                new[]
                {
                    "folders", "shared-assembly", "signals-assembly", "assembly", "references", "signal-references",
                    "dotsettings", "module-card"
                },
                ids);
        }

        /// <summary>
        /// The index is refreshed first because everything downstream reads it, and the orphan
        /// sweep runs after the assemblies are known so a newly written one is not mistaken for
        /// a stray file.
        /// </summary>
        [Test]
        public void Project_checks_run_index_first_and_orphans_after_the_assemblies_are_known()
        {
            List<string> ids = new ModuleCheckPipeline().ProjectChecks.Select(check => check.Id).ToList();

            CollectionAssert.AreEqual(
                new[] {"index", "module-directory", "orphans", "log-types", "code-style"}, ids);
        }

        /// <summary>
        /// The repair matches a finding back to its check by id, so two checks sharing one would
        /// have the wrong repair run against the wrong finding.
        /// </summary>
        [Test]
        public void No_check_appears_twice()
        {
            var pipeline = new ModuleCheckPipeline();

            List<string> ids = pipeline.ModuleChecks.Select(check => check.Id)
                .Concat(pipeline.ProjectChecks.Select(check => check.Id))
                .ToList();

            CollectionAssert.AllItemsAreUnique(ids);
        }
    }
}