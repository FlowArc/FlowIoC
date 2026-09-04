#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One rule about the project rather than about a module. The module index, the orphaned
    /// settings files, the Flow log types and the solution code style all belong to the project
    /// as a whole, so hanging them off an arbitrary module's row would misreport them.
    /// </summary>
    internal interface IProjectCheck
    {
        string Id { get; }
        FindingEVO Inspect(ProjectTargetEVO project);
        void Fix(ProjectTargetEVO project);
    }
}

#endif
