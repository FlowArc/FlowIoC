#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// Settings and project files at the root whose assembly no longer exists. A module that was
    /// renamed or deleted leaves its .csproj.DotSettings behind, and the stale file goes on
    /// telling Rider about folders nothing owns.
    ///
    /// This is the only repair in the pipeline that deletes, and it deletes only files whose name
    /// matches no assembly anywhere in Assets or Packages. A project whose assemblies could not
    /// be listed is not a project with no assemblies, so an empty list sweeps nothing.
    /// </summary>
    internal class OrphanFilesCheck : IProjectCheck
    {
        private const string SETTINGS_PATTERN = "*.csproj.DotSettings";
        private const string SETTINGS_SUFFIX = ".csproj.DotSettings";
        private const string PROJECT_PATTERN = "Modules.*.csproj";

        private readonly Func<string, string, string[]> _filesMatching;
        private readonly Action<string> _deleteFile;

        internal OrphanFilesCheck() : this(
            (root, pattern) => Directory.Exists(root)
                ? Directory.GetFiles(root, pattern, SearchOption.TopDirectoryOnly)
                : new string[0],
            File.Delete)
        {
        }

        internal OrphanFilesCheck(Func<string, string, string[]> filesMatching, Action<string> deleteFile)
        {
            _filesMatching = filesMatching;
            _deleteFile = deleteFile;
        }

        public string Id => "orphans";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            List<string> orphans = Orphans(project);

            if (orphans.Count == 0)
                return FindingEVO.Ok(Id, "No orphaned settings files");

            var names = new List<string>();

            foreach (string path in orphans)
                names.Add(Path.GetFileName(path));

            return FindingEVO.Fixable(Id, $"{orphans.Count} orphaned: {string.Join(", ", names)}");
        }

        public void Fix(ProjectTargetEVO project)
        {
            foreach (string path in Orphans(project))
                _deleteFile(path);
        }

        private List<string> Orphans(ProjectTargetEVO project)
        {
            var orphans = new List<string>();

            if (project?.AllAssemblyNames == null || project.AllAssemblyNames.Count == 0)
                return orphans;

            var known = new HashSet<string>(project.AllAssemblyNames, StringComparer.Ordinal);

            foreach (string path in _filesMatching(project.ProjectRoot, SETTINGS_PATTERN))
            {
                string assembly = Path.GetFileName(path).Replace(SETTINGS_SUFFIX, string.Empty);

                if (!known.Contains(assembly)) orphans.Add(path);
            }

            foreach (string path in _filesMatching(project.ProjectRoot, PROJECT_PATTERN))
            {
                if (!known.Contains(Path.GetFileNameWithoutExtension(path))) orphans.Add(path);
            }

            return orphans;
        }
    }
}

#endif
