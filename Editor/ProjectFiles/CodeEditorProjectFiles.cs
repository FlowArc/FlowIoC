#if UNITY_EDITOR
using Unity.CodeEditor;

namespace FlowIoC.Editor.ProjectFiles
{
    /// <summary>
    /// The regenerator Unity offers: the external code editor's own SyncAll, which is what the
    /// Regenerate project files button in Preferences calls. Rider, Visual Studio and VS Code
    /// each implement it; an Editor with no external editor set has nothing to sync and says so.
    /// </summary>
    internal class CodeEditorProjectFiles : IProjectFilesRegenerator
    {
        public string EditorName => CodeEditor.CurrentEditor?.GetType().Name ?? "no code editor";

        public void Regenerate() => CodeEditor.CurrentEditor?.SyncAll();
    }
}
#endif
