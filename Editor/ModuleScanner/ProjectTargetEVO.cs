#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// What the project-wide checks are pointed at. The scanned modules and the stored index are
    /// both here because the index check's whole job is to compare one against the other.
    /// </summary>
    internal class ProjectTargetEVO
    {
        internal string ProjectRoot { get; set; }
        internal IReadOnlyList<string> AllAssemblyNames { get; set; }
        internal IReadOnlyList<ScannedModule> ScannedModules { get; set; }
        internal ED_ModuleIndex Index { get; set; }
        internal IReadOnlyList<string> RegisteredAutoLogTypes { get; set; }
    }
}

#endif
