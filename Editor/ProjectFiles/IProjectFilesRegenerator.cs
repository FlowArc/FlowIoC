#if UNITY_EDITOR
namespace FlowIoC.Editor.ProjectFiles
{
    /// <summary>
    /// Whatever rewrites the IDE's solution and project files - the code editor Unity is set to,
    /// behind an interface so the rule that decides when to call it can be tested without one.
    /// </summary>
    internal interface IProjectFilesRegenerator
    {
        /// <summary>The name of the editor that will do it, for the line that says it was done.</summary>
        string EditorName { get; }

        /// <summary>Rewrites every project file and the solution, the way Regenerate project files does.</summary>
        void Regenerate();
    }
}
#endif
