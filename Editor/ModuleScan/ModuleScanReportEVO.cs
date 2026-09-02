#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// A whole scan: the findings that belong to the project rather than to any one module, and
    /// a row per module. The project findings are separate because there is no module to hang
    /// the module index or the solution code style on.
    /// </summary>
    internal class ModuleScanReportEVO
    {
        internal List<FindingEVO> Project { get; } = new List<FindingEVO>();
        internal List<ModuleRowEVO> Modules { get; } = new List<ModuleRowEVO>();

        internal int IssueCount
        {
            get
            {
                int count = 0;

                foreach (FindingEVO finding in Project)
                {
                    if (finding.Status != ModuleCheckStatus.Ok) count++;
                }

                foreach (ModuleRowEVO row in Modules)
                foreach (FindingEVO finding in row.Findings)
                {
                    if (finding.Status != ModuleCheckStatus.Ok) count++;
                }

                return count;
            }
        }

        internal int ModulesWithIssues
        {
            get
            {
                int count = 0;

                foreach (ModuleRowEVO row in Modules)
                {
                    if (row.Status != ModuleCheckStatus.Ok) count++;
                }

                return count;
            }
        }
    }
}

#endif
