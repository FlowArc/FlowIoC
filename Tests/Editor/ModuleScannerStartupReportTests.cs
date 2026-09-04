using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleScannerStartupReportTests
    {
        private static ModuleScannerReportEVO ReportWith(params ModuleCheckStatus[] moduleStatuses)
        {
            var report = new ModuleScannerReportEVO();

            for (int index = 0; index < moduleStatuses.Length; index++)
            {
                var row = new ModuleRowEVO {Name = "Module" + index, Kind = ModuleKind.Main};
                row.Findings.Add(new FindingEVO("check", moduleStatuses[index], "message"));
                report.Modules.Add(row);
            }

            return report;
        }

        /// <summary>
        /// A clean project says nothing. A line on every editor load that only ever reads
        /// "0 issues" is noise, and noise is what teaches people to stop reading the console.
        /// </summary>
        [Test]
        public void A_clean_project_produces_no_line()
        {
            Assert.IsNull(new ModuleScannerStartupReport().LineFor(ReportWith(ModuleCheckStatus.Ok)));
        }

        [Test]
        public void A_report_that_does_not_exist_produces_no_line()
        {
            Assert.IsNull(new ModuleScannerStartupReport().LineFor(null));
        }

        [Test]
        public void The_line_counts_the_issues_the_modules_they_are_in_and_names_the_menu()
        {
            string line = new ModuleScannerStartupReport()
                .LineFor(ReportWith(ModuleCheckStatus.Fixable, ModuleCheckStatus.Manual, ModuleCheckStatus.Ok));

            StringAssert.Contains("2 issues", line);
            StringAssert.Contains("2 modules", line);
            StringAssert.Contains("Tools/FlowIoC/Module Scanner", line);
        }

        /// <summary>
        /// The module index, the orphaned settings files and the solution code style belong to no
        /// module, so a project whose modules are all fine can still have something wrong.
        /// </summary>
        [Test]
        public void A_project_level_issue_alone_still_produces_a_line()
        {
            var report = new ModuleScannerReportEVO();
            report.Project.Add(FindingEVO.Fixable("orphans", "2 orphaned"));

            Assert.IsNotNull(new ModuleScannerStartupReport().LineFor(report));
        }
    }
}
