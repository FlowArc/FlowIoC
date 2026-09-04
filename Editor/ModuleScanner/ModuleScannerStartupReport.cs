#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One line on the console when the project has something wrong with it, so the panel does
    /// not have to be remembered to be useful.
    ///
    /// A clean project says nothing. A line on every load that only ever reads "0 issues" is what
    /// teaches people to stop reading the console, and the whole point of this one is that it is
    /// worth reading when it appears.
    /// </summary>
    internal class ModuleScannerStartupReport
    {
        internal string LineFor(ModuleScannerReportEVO report)
        {
            if (report == null || report.IssueCount == 0) return null;

            return "<color=cyan>FlowIoC:</color> Module Scanner found "
                   + $"{report.IssueCount} issues across {report.ModulesWithIssues} modules "
                   + "- Tools/FlowIoC/Module Scanner";
        }

        internal void Report()
        {
            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            string line = LineFor(new ModuleScannerRunner(new ModuleCheckPipeline()).Run(project, modules));

            if (line != null) Debug.LogWarning(line);
        }
    }
}

#endif
