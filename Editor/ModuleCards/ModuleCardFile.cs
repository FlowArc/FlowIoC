#if UNITY_EDITOR

using System.IO;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Where a module's card sits and how it is read. The name is fixed rather than derived from
    /// the module, so that every card in a project is found with one glob - which is what the
    /// fallback in AgentRules.md tells an agent to do when the directory is absent.
    /// </summary>
    internal class ModuleCardFile
    {
        internal const string FILE_NAME = "MODULE.md";

        internal string PathFor(string moduleAbsolutePath) => Path.Combine(moduleAbsolutePath, FILE_NAME);

        internal bool Exists(string moduleAbsolutePath) => File.Exists(PathFor(moduleAbsolutePath));

        internal string Read(string moduleAbsolutePath)
        {
            string path = PathFor(moduleAbsolutePath);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        internal void Write(string moduleAbsolutePath, string text) =>
            File.WriteAllText(PathFor(moduleAbsolutePath), text);
    }
}

#endif
