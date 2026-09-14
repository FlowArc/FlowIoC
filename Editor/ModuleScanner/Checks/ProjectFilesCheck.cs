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
        private readonly IProjectFilesRegenerator _regenerator;

        internal ProjectFilesCheck() : this(
            (root, pattern) => Directory.Exists(root)
                ? Directory.GetFiles(root, pattern, SearchOption.TopDirectoryOnly)
                : new string[0],
            File.ReadAllText,
            Directory.Exists,
            File.Exists,
            new CodeEditorProjectFiles())
        {
        }

        internal ProjectFilesCheck(Func<string, string, string[]> filesMatching, Func<string, string> readText,
            Func<string, bool> directoryExists, Func<string, bool> fileExists, IProjectFilesRegenerator regenerator)
        {
            _filesMatching = filesMatching;
            _readText = readText;
            _directoryExists = directoryExists;
            _fileExists = fileExists;
            _regenerator = regenerator;
        }

        public string Id => "project-files";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            List<string> stale = Stale(project);

            if (stale.Count == 0)
                return FindingEVO.Ok(Id, "Project files follow the packages");

            return FindingEVO.Fixable(Id,
                $"{stale.Count} project file(s) point at a package folder or a project that is gone - "
                + $"the IDE reports what Unity compiles: {string.Join(", ", stale)}");
        }

        public void Fix(ProjectTargetEVO project) => _regenerator.Regenerate();

        private List<string> Stale(ProjectTargetEVO project)
        {
            var stale = new List<string>();
            if (string.IsNullOrEmpty(project?.ProjectRoot))
                return stale;

            string root = project.ProjectRoot;

            foreach (string path in _filesMatching(root, PROJECT_PATTERN))
            {
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
