#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One module's row in the panel: what it is, and what every check said about it.
    /// </summary>
    internal class ModuleRowEVO
    {
        internal string Name { get; set; }
        internal ModuleKind Kind { get; set; }
        internal string AssetPath { get; set; }
        internal string AssemblyName { get; set; }
        internal List<FindingEVO> Findings { get; } = new List<FindingEVO>();

        /// <summary>
        /// The worst status among the findings, so a single Manual finding turns the whole
        /// module red rather than hiding behind four green ones. A module with no findings is
        /// Ok rather than unknown: every check answers, even if only to say it does not apply.
        /// </summary>
        internal ModuleCheckStatus Status
        {
            get
            {
                ModuleCheckStatus worst = ModuleCheckStatus.Ok;

                foreach (FindingEVO finding in Findings)
                {
                    if (finding.Status > worst) worst = finding.Status;
                }

                return worst;
            }
        }
    }
}

#endif
