#if UNITY_EDITOR

using System.IO;
using FlowIoC.BaseModule.ProjectPaths;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The generated directory, and the folder it lives in. It sits under Assets/Plugins/FlowIoC
    /// rather than at the project root because the file is not committed, and the rule that keeps
    /// it out of history has to sit beside it - which at the root would mean editing the
    /// consuming project's own .gitignore. FlowIoC owns this folder; the root belongs to the
    /// project, and ModuleIndexIgnoreRule draws the same line for the module index.
    ///
    /// A path an agent has to be told about costs nothing here, because AgentRules.md is already
    /// the thing telling it where to look.
    /// </summary>
    internal class ModuleDirectoryFile
    {
        internal const string FILE_NAME = "MODULES.md";

        private readonly FlowIoCProjectPaths _paths = new FlowIoCProjectPaths();

        /// <summary>Assets/Plugins/FlowIoC, as an absolute path.</summary>
        internal string FolderFor(string projectRoot) => Path.Combine(projectRoot, _paths.Root);

        internal string PathFor(string projectRoot) => Path.Combine(FolderFor(projectRoot), FILE_NAME);

        internal string Read(string projectRoot)
        {
            string path = PathFor(projectRoot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        internal void Write(string projectRoot, string text)
        {
            string folder = FolderFor(projectRoot);

            // The folder is FlowIoC's own and exists in any project that has run its setup, but a
            // directory that is not there yet must not cost the whole scan an exception.
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            File.WriteAllText(PathFor(projectRoot), text);
        }
    }
}

#endif