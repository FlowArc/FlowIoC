#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Runs the pipeline over the targets it is handed and returns the report. It does not find
    /// its own targets - ModuleTargetFactory does - which keeps this class free of what happens
    /// to be on disk and lets a test describe exactly the project it means. That is the same
    /// division ScreenScannerRunner makes with the Roots it is given.
    ///
    /// Nothing here writes. A scan is what the window runs on focus, so it has to be free.
    /// </summary>
    internal class ModuleScannerRunner
    {
        private readonly ModuleCheckPipeline _pipeline;

        internal ModuleScannerRunner(ModuleCheckPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        internal ModuleScannerReportEVO Run(ProjectTargetEVO project, IEnumerable<ModuleTargetEVO> modules)
        {
            var report = new ModuleScannerReportEVO();

            foreach (IProjectCheck check in _pipeline.ProjectChecks)
            {
                IProjectCheck captured = check;
                report.Project.Add(Inspected(check.Id, () => captured.Inspect(project)));
            }

            foreach (ModuleTargetEVO module in modules)
            {
                var row = new ModuleRowEVO
                {
                    Name = module.Name,
                    Kind = module.Kind,
                    AssetPath = module.AssetPath,
                    AssemblyName = module.ExpectedAssemblyName
                };

                foreach (IModuleCheck check in _pipeline.ModuleChecks)
                {
                    IModuleCheck captured = check;
                    ModuleTargetEVO target = module;

                    row.Findings.Add(Inspected(check.Id, () => captured.Inspect(target)));
                }

                report.Modules.Add(row);
            }

            return report;
        }

        /// <summary>
        /// A check that throws is a broken check, not a broken report. It becomes the finding it
        /// failed to produce, so the panel shows the failure rather than showing nothing at all.
        /// </summary>
        private FindingEVO Inspected(string checkId, Func<FindingEVO> inspect)
        {
            try
            {
                return inspect() ?? FindingEVO.Ok(checkId, checkId);
            }
            catch (Exception exception)
            {
                return FindingEVO.Manual(checkId, $"The check itself failed: {exception.Message}");
            }
        }
    }
}

#endif
