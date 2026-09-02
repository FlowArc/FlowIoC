#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Which of the folders between a module and one of its files add a segment to that file's
    /// namespace. Scripts and Scripts/Runtime are structure, so a signal holder under
    /// Scripts/Shared/Signals is in Modules.PlayerModule.Shared.Signals.
    ///
    /// The folders that name nothing are handed in rather than looked up, because the list is
    /// DotSettingsPlan's to answer - it is the same list written into the module's
    /// .csproj.DotSettings, so the namespace the generator writes and the one Rider expects
    /// cannot disagree.
    /// </summary>
    internal class FolderNamespaceSegments
    {
        internal IReadOnlyList<string> Between(
            string moduleFolder,
            string fileDirectory,
            IReadOnlyCollection<string> skipFolders)
        {
            var segments = new List<string>();

            string module = Normalize(moduleFolder);
            string directory = Normalize(fileDirectory);

            if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(directory)) return segments;
            if (!directory.StartsWith(module + "/", StringComparison.OrdinalIgnoreCase)) return segments;

            HashSet<string> skip = SkipSet(skipFolders);

            string currentPath = module;

            foreach (string folderName in directory.Substring(module.Length + 1).Split('/'))
            {
                if (string.IsNullOrEmpty(folderName)) continue;

                currentPath += "/" + folderName;

                if (!skip.Contains(currentPath))
                    segments.Add(folderName);
            }

            return segments;
        }

        private HashSet<string> SkipSet(IReadOnlyCollection<string> skipFolders)
        {
            var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (skipFolders == null) return skip;

            foreach (string folder in skipFolders)
                skip.Add(Normalize(folder));

            return skip;
        }

        /// <summary>
        /// The two sides of the comparison are assembled by different callers - one from Unity's
        /// forward slashes, one from Path.Combine's backslashes - so both are reduced to the same
        /// shape before they ever meet.
        /// </summary>
        private string Normalize(string path)
        {
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : path.Replace('\\', '/').TrimEnd('/');
        }
    }
}

#endif