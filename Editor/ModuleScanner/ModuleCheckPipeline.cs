#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The order the checks run in, declared once.
    ///
    /// Both the scan and the repair read this list, and the repair's correctness depends on the
    /// order: Scripts/Shared and Scripts/Signals have to exist before their asmdefs can be
    /// written, the module asmdef references both so both come first, the Signals assembly
    /// references Shared so Shared comes before it, references are added to an asmdef that must
    /// already exist, and the DotSettings file name derives from the final assembly name. The
    /// signal-reference check reads a finished reference list, so it comes after the one that
    /// adds to it, the log type part is checked once the module's own assemblies are settled
    /// because its asmref is about which assembly the part lands in, and the card is written last
    /// because its block names the assemblies every check above it settles. On the project side the index is refreshed first, and the orphan
    /// sweep runs after the assemblies are known so that a newly written one is not mistaken for
    /// a stray file.
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
                new SignalsAssemblyCheck(),
                new AssemblyDefinitionCheck(),
                new AssemblyReferencesCheck(),
                new SignalReferenceCheck(),
                new DotSettingsCheck(),
                new LogTypePartCheck(),
                new ModuleCardCheck()
            },
            new IProjectCheck[]
            {
                new ModuleIndexCheck(),
                new ModuleDirectoryCheck(),
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