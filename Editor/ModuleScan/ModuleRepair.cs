#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// Applies the Fixable findings of a report. Findings are not applied in report order,
    /// because the repairs depend on one another - see ModuleCheckPipeline for what depends on
    /// what. A check also finishes across every module before the next check starts, because a
    /// later check may read what an earlier one wrote for a different module.
    ///
    /// Each Fix is guarded on its own, the way NamespaceProvider.RunGuarded guarded its closing
    /// steps: one module the file system will not let us write must not cost the rest of the run.
    /// </summary>
    internal class ModuleRepair
    {
        private readonly ModuleCheckPipeline _pipeline;

        internal ModuleRepair() : this(new ModuleCheckPipeline())
        {
        }

        internal ModuleRepair(ModuleCheckPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// Scan, then repair, then let Unity see what changed. This is the entry point for
        /// callers that have no report of their own - the module installer, the setup startup,
        /// the code generator settings editor - and for the panel's Fix All button.
        /// </summary>
        internal RepairResultEVO FixAll()
        {
            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            ModuleScanReportEVO report = new ModuleScanRunner(_pipeline).Run(project, modules);
            RepairResultEVO result = Apply(report, project, modules);

            AssetDatabase.Refresh();

            return result;
        }

        /// <summary>
        /// <paramref name="modules"/> must be the list the report was produced from: the rows
        /// are matched to the targets by position, which is what the runner guarantees by
        /// walking the same list.
        /// </summary>
        internal RepairResultEVO Apply(
            ModuleScanReportEVO report,
            ProjectTargetEVO project,
            IReadOnlyList<ModuleTargetEVO> modules)
        {
            var result = new RepairResultEVO();

            foreach (IProjectCheck check in _pipeline.ProjectChecks)
            {
                IProjectCheck captured = check;

                Act(Find(report.Project, check.Id), result, check.Id, "The project",
                    () => captured.Fix(project));
            }

            foreach (IModuleCheck check in _pipeline.ModuleChecks)
            {
                for (int index = 0; index < modules.Count && index < report.Modules.Count; index++)
                {
                    IModuleCheck captured = check;
                    ModuleTargetEVO target = modules[index];

                    Act(Find(report.Modules[index].Findings, check.Id), result, check.Id, target.Name,
                        () => captured.Fix(target));
                }
            }

            return result;
        }

        private void Act(FindingEVO finding, RepairResultEVO result, string checkId, string subject, Action fix)
        {
            if (finding == null || finding.Status == ModuleCheckStatus.Ok) return;

            if (finding.Status == ModuleCheckStatus.Manual)
            {
                result.Remaining.Add($"{subject}: {finding.Message}");
                return;
            }

            try
            {
                fix();
                result.Fixed++;
            }
            catch (Exception exception)
            {
                result.Remaining.Add($"{subject}: {checkId} could not be repaired - {exception.Message}");
            }
        }

        private FindingEVO Find(List<FindingEVO> findings, string checkId)
        {
            foreach (FindingEVO finding in findings)
            {
                if (finding.CheckId == checkId) return finding;
            }

            return null;
        }
    }
}

#endif