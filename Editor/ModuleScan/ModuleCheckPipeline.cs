#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// The order the checks run in, declared once.
    ///
    /// Both the scan and the repair read this list, and the repair's correctness depends on the
    /// order: Scripts/Shared has to exist before its asmdef can be written, the module asmdef
    /// references the Shared assembly so Shared comes first, references are added to an asmdef
    /// that must already exist, and the DotSettings file name derives from the final assembly
    /// name. On the project side the index is refreshed first, and the orphan sweep runs after
    /// the assemblies are known so that a newly written one is not mistaken for a stray file.
    ///
    /// The order lives here rather than as an Order property on each check, so that reading it
    /// means opening one file.
    /// </summary>
    internal class ModuleCheckPipeline
    {
        internal IReadOnlyList<IModuleCheck> ModuleChecks { get; }
        internal IReadOnlyList<IProjectCheck> ProjectChecks { get; }

        internal ModuleCheckPipeline() : this(
            new IModuleCheck[]
            {
                new MandatoryFoldersCheck(),
                new SharedAssemblyCheck(),
                new AssemblyDefinitionCheck(),
                new AssemblyReferencesCheck(),
                new DotSettingsCheck()
            },
            new IProjectCheck[]
            {
                new ModuleIndexCheck(),
                new OrphanFilesCheck(),
                new LogTypeCheck(),
                new SolutionCodeStyleCheck()
            })
        {
        }

        internal ModuleCheckPipeline(
            IReadOnlyList<IModuleCheck> moduleChecks,
            IReadOnlyList<IProjectCheck> projectChecks)
        {
            ModuleChecks = moduleChecks;
            ProjectChecks = projectChecks;
        }
    }
}

#endif