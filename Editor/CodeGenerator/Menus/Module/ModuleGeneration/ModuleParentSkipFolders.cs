#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The z folders a new module hangs under - zSubModules, zScreenModules, zTestModules on its
    /// way down from Assets - each as the project-relative folder Rider is told to skip.
    ///
    /// The module's path is read through NamespaceUtility.ProjectRelativePath, which strips the
    /// project root without case. A plain Replace of Application.dataPath missed a parent a script
    /// had resolved against the Editor's working directory in another casing; the path then split
    /// into its drive and every folder above the project, and the keys came out absolute -
    /// D:Work_005CCNYT_005C... - so Rider never skipped the folder and flagged every namespace
    /// under it.
    /// </summary>
    internal class ModuleParentSkipFolders
    {
        internal IReadOnlyList<string> For(string modulePath, ICollection<string> skipFolderNames)
        {
            var folders = new List<string>();

            string[] segments = NamespaceUtility.ProjectRelativePath(modulePath)
                .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            string current = string.Empty;

            foreach (string segment in segments)
            {
                current = current.Length == 0 ? segment : current + "/" + segment;

                if (skipFolderNames.Contains(segment))
                    folders.Add(current);
            }

            return folders;
        }
    }
}
#endif
