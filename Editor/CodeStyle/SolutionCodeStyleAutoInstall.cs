#if UNITY_EDITOR

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

            if (report.WrittenPath != null)
                Debug.Log($"[FlowIoC] Solution code style written: {Path.GetFileName(report.WrittenPath)}");
        }
    }

    /// <summary>
    /// What one automatic run did. <see cref="WrittenPath"/> is null when the file already matched
    /// what the package ships, which is every session after the first.
    /// </summary>
    internal readonly struct SolutionCodeStyleReport
    {
        internal string WrittenPath { get; }
        internal string Error { get; }

        internal SolutionCodeStyleReport(string writtenPath, string error)
        {
            WrittenPath = writtenPath;
            Error = error;
        }
    }

    /// <summary>
    /// Decides whether the solution code style needs writing and writes it. Separate from the
    /// startup hook so the decision can be tested against a temporary directory instead of an
    /// Editor session.
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

            if (!writer.TryWrite(out string path, out string error, out bool changed))
                return new SolutionCodeStyleReport(null, error);

            return new SolutionCodeStyleReport(changed ? path : null, null);
        }
    }
}

#endif