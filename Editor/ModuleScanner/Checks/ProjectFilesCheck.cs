#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FlowIoC.Editor.ProjectFiles;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Project files at the root that describe a project that is gone. A package update lands in
    /// a new folder under Library/PackageCache and the IDE integration's incremental sync leaves
    /// the package's own .csproj on the old one; a solution that still lists a project file that
    /// has been swept is the same thing from the other side. Unity compiles from neither, so the
    /// project builds while the IDE reports every type in the package unresolved.
    ///
    /// The repair is the one Preferences offers by hand: regenerate every project file. The
    /// startup sync presses it once per FlowIoC version on its own; this check is where the
    /// state shows, and the Fix for the cases the startup could not cover.
    ///
    /// Regenerating writes the projects Unity generates today and never deletes the rest: an
    /// assembly that was removed, or a package whose projects the IDE no longer generates, leaves
    /// a .csproj no solution lists. Nothing loads it, so the Fix deletes it once the solution is
    /// written afresh.
    /// </summary>
    internal class ProjectFilesCheck : IProjectCheck
    {
        private const string PROJECT_PATTERN = "*.csproj";
        private const string SOLUTION_PATTERN = "*.sln";
        private const string PACKAGE_CACHE = "Library/PackageCache";

        private static readonly Regex PackageFolder =
            new Regex(@"PackageCache[\\/]([^\\/""<>]+@[0-9a-f]+)", RegexOptions.Compiled);

        private static readonly Regex SolutionProject =
            new Regex(@"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""([^""]+\.csproj)""",
                RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly Func<string, string, string[]> _filesMatching;
        private readonly Func<string, string> _readText;
        private readonly Func<string, bool> _directoryExists;
        private readonly Func<string, bool> _fileExists;
        private readonly Action<string> _deleteFile;
        private readonly IProjectFilesRegenerator _regenerator;

        internal ProjectFilesCheck() : this(
            (root, pattern) => Directory.Exists(root)
                ? Directory.GetFiles(root, pattern, SearchOption.TopDirectoryOnly)
                : new string[0],
            File.ReadAllText,
            Directory.Exists,
            File.Exists,
            File.Delete,
            new CodeEditorProjectFiles())
        {
        }

        internal ProjectFilesCheck(Func<string, string, string[]> filesMatching, Func<string, string> readText,
            Func<string, bool> directoryExists, Func<string, bool> fileExists, Action<string> deleteFile,
            IProjectFilesRegenerator regenerator)
        {
            _filesMatching = filesMatching;
            _readText = readText;
            _directoryExists = directoryExists;
            _fileExists = fileExists;
            _deleteFile = deleteFile;
            _regenerator = regenerator;
        }

        public string Id => "project-files";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            if (string.IsNullOrEmpty(project?.ProjectRoot))
                return FindingEVO.Ok(Id, "Project files follow the packages");

            string root = project.ProjectRoot;
            List<string> unlisted = Unlisted(root);
            List<string> stale = Stale(root, unlisted);

            if (stale.Count == 0 && unlisted.Count == 0)
                return FindingEVO.Ok(Id, "Project files follow the packages");

            var parts = new List<string>();

            if (stale.Count > 0)
                parts.Add($"{stale.Count} project file(s) point at a package folder or a project that is gone - "
                          + $"the IDE reports what Unity compiles: {string.Join(", ", stale)}");

            if (unlisted.Count > 0)
                parts.Add($"{unlisted.Count} project file(s) no solution lists, so nothing loads them: "
                          + string.Join(", ", Names(unlisted)));

            return FindingEVO.Fixable(Id, string.Join("; ", parts));
        }

        public void Fix(ProjectTargetEVO project)
        {
            _regenerator.Regenerate();

            if (string.IsNullOrEmpty(project?.ProjectRoot))
                return;

            foreach (string path in Unlisted(project.ProjectRoot))
                _deleteFile(path);
        }

        /// <summary>
        /// The root project files that no solution at the root lists. With no solution at all
        /// there is nothing to measure against - the IDE has not synced yet - so none is.
        /// </summary>
        private List<string> Unlisted(string root)
        {
            var unlisted = new List<string>();
            string[] solutions = _filesMatching(root, SOLUTION_PATTERN);
            if (solutions.Length == 0)
                return unlisted;

            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string solution in solutions)
            {
                foreach (Match match in SolutionProject.Matches(_readText(solution) ?? string.Empty))
                    listed.Add(Path.GetFileName(match.Groups[1].Value));
            }

            foreach (string path in _filesMatching(root, PROJECT_PATTERN))
            {
                if (!listed.Contains(Path.GetFileName(path)))
                    unlisted.Add(path);
            }

            return unlisted;
        }

        private static IEnumerable<string> Names(List<string> paths)
        {
            foreach (string path in paths)
                yield return Path.GetFileName(path);
        }

        private List<string> Stale(string root, List<string> unlisted)
        {
            var stale = new List<string>();

            foreach (string path in _filesMatching(root, PROJECT_PATTERN))
            {
                // A project file nothing loads is named once, as that, and the Fix deletes it.
                if (unlisted.Contains(path)) continue;
                if (PointsAtMissingPackage(root, _readText(path)))
                    stale.Add(Path.GetFileName(path));
            }

            foreach (string path in _filesMatching(root, SOLUTION_PATTERN))
            {
                if (ListsMissingProject(root, _readText(path)))
                    stale.Add(Path.GetFileName(path));
            }

            return stale;
        }

        /// <summary>
        /// One missing folder is enough: every source line of the package's project points at
        /// the same folder, and the first tells the story.
        /// </summary>
        private bool PointsAtMissingPackage(string root, string projectText)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match match in PackageFolder.Matches(projectText ?? string.Empty))
            {
                string folder = match.Groups[1].Value;
                if (!seen.Add(folder)) continue;
                if (!_directoryExists(Path.Combine(root, PACKAGE_CACHE, folder)))
                    return true;
            }

            return false;
        }

        private bool ListsMissingProject(string root, string solutionText)
        {
            foreach (Match match in SolutionProject.Matches(solutionText ?? string.Empty))
            {
                if (!_fileExists(Path.Combine(root, match.Groups[1].Value)))
                    return true;
            }

            return false;
        }
    }
}
#endif