#if UNITY_EDITOR

using System;
using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="SolutionCodeStyleStartup"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class SolutionCodeStyleStartupHook
    {
        static SolutionCodeStyleStartupHook()
        {
            EditorApplication.delayCall += () => new SolutionCodeStyleStartup().Run();
        }
    }

    /// <summary>
    /// Writes the code style FlowIoC ships into the consumer project as soon as the Editor opens.
    ///
    /// The rules that decide what a `CD_` asset or a `PVO` value object may be called live in the
    /// solution level settings file, and Rider only reads it under the solution's own name. Until
    /// this ran, that file was written by a menu item the reader had to know about and nothing
    /// else - it is `Tools/FlowIoC/Module Scanner` now - so a project that installed the package and generated a module
    /// had every convention documented and none of them enforced. The rules ship with the package;
    /// they should arrive with it, the way the agent rules and the skills already do.
    ///
    /// Only the keys FlowIoC ships are touched, so a team's own settings survive, and a session
    /// that finds the file already correct writes nothing and says nothing.
    ///
    /// A settings file named after a solution that is not there is swept on the same run. A project
    /// folder renamed once - a game cloned from a template repository is the common case - leaves
    /// the old solution's file beside the new one, and Rider goes on reading whichever it opens.
    /// The Module Scanner reports and sweeps the same file; this is the sweep nobody has to ask for.
    /// </summary>
    internal class SolutionCodeStyleStartup
    {
        private const string SessionKey = "FlowIoC.SolutionCodeStyle.Written";

        internal void Run()
        {
            // A batch run has no one to write for and no business editing the workspace it was
            // handed - the same line the agent skills install draws.
            if (Application.isBatchMode)
                return;

            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);

            SolutionCodeStyleReport report = new SolutionCodeStyleAutoInstall(
                new ProjectRoot().Resolve(),
                new PackageCodeStyleTemplate().Resolve()).Run();

            if (report.Error != null)
            {
                Debug.LogWarning($"[FlowIoC] The solution code style could not be written: {report.Error}");
                return;
            }

            foreach (string removed in report.RemovedPaths)
                Debug.Log($"[FlowIoC] Orphaned solution code style deleted: {Path.GetFileName(removed)}");

            if (report.WrittenPath != null)
                Debug.Log($"[FlowIoC] Solution code style written: {Path.GetFileName(report.WrittenPath)}");
        }
    }

    /// <summary>
    /// What one automatic run did. <see cref="WrittenPath"/> is null when the file already matched
    /// what the package ships, which is every session after the first; <see cref="RemovedPaths"/>
    /// is empty unless a settings file was left behind by a solution that is gone.
    /// </summary>
    internal readonly struct SolutionCodeStyleReport
    {
        internal string WrittenPath { get; }
        internal string[] RemovedPaths { get; }
        internal string Error { get; }

        internal SolutionCodeStyleReport(string writtenPath, string[] removedPaths, string error)
        {
            WrittenPath = writtenPath;
            RemovedPaths = removedPaths ?? Array.Empty<string>();
            Error = error;
        }
    }

    /// <summary>
    /// Sweeps the settings of any solution that is gone, then decides whether the solution code
    /// style needs writing and writes it. Separate from the startup hook so both can be tested
    /// against a temporary directory instead of an Editor session.
    /// </summary>
    internal class SolutionCodeStyleAutoInstall
    {
        private readonly string _projectRoot;
        private readonly string _templatePath;

        internal SolutionCodeStyleAutoInstall(string projectRoot, string templatePath)
        {
            _projectRoot = projectRoot;
            _templatePath = templatePath;
        }

        internal SolutionCodeStyleReport Run()
        {
            var writer = new SolutionDotSettingsWriter(_projectRoot, _templatePath);

            // Swept before the write, and only against a solution that exists: with no .sln at the
            // root there is nothing to compare a settings file against, and the writer leaves every
            // one of them alone rather than guess.
            string[] removed = writer.CleanupOrphaned();

            if (!writer.TryWrite(out string path, out string error, out bool changed))
                return new SolutionCodeStyleReport(null, removed, error);

            return new SolutionCodeStyleReport(changed ? path : null, removed, null);
        }
    }
}

#endif