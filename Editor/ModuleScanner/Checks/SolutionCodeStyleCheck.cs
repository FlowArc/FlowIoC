#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeStyle;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The solution-wide .sln.DotSettings: the naming rules, the CD_/RD_/PD_ prefixes and the
    /// VO/CVO/RVO suffixes the project is written against. It is generated from the package's
    /// template rather than edited by hand, so a difference means the template moved on.
    ///
    /// Settings files belonging to a solution that no longer exists are swept with it - a project
    /// folder renamed once leaves one behind, and Rider goes on reading it.
    /// </summary>
    internal class SolutionCodeStyleCheck : IProjectCheck
    {
        private readonly Func<SolutionState> _state;
        private readonly Action _write;

        internal SolutionCodeStyleCheck() : this(DefaultState, DefaultWrite)
        {
        }

        internal SolutionCodeStyleCheck(Func<SolutionState> state, Action write)
        {
            _state = state;
            _write = write;
        }

        public string Id => "code-style";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            SolutionState state = _state();

            if (!string.IsNullOrEmpty(state.Error))
                return FindingEVO.Manual(Id, state.Error);

            var parts = new List<string>();

            if (state.Drifted)
                parts.Add("the solution's .sln.DotSettings does not match the FlowIoC template");

            if (state.Orphaned.Count > 0)
                parts.Add($"orphaned: {string.Join(", ", state.Orphaned)}");

            if (parts.Count == 0)
                return FindingEVO.Ok(Id, "Solution code style");

            return FindingEVO.Fixable(Id, $"Solution code style - {string.Join("; ", parts)}");
        }

        public void Fix(ProjectTargetEVO project) => _write();

        /// <summary>
        /// What one read of the solution's settings found. Kept as a value rather than three
        /// delegates so a test describes one situation at a time.
        /// </summary>
        internal class SolutionState
        {
            internal bool Drifted { get; set; }
            internal List<string> Orphaned { get; } = new List<string>();
            internal string Error { get; set; }
        }

        private static SolutionState DefaultState()
        {
            SolutionDotSettingsWriter writer = WriterFor();
            var state = new SolutionState();

            if (!writer.TryCompose(out string _, out string _, out bool changed, out string error))
            {
                state.Error = error;

                return state;
            }

            state.Drifted = changed;

            foreach (string orphan in writer.Orphaned())
                state.Orphaned.Add(Path.GetFileName(orphan));

            return state;
        }

        private static void DefaultWrite()
        {
            SolutionDotSettingsWriter writer = WriterFor();

            foreach (string removed in writer.CleanupOrphaned())
                Debug.Log($"[Module Scanner] Orphaned solution DotSettings deleted: {Path.GetFileName(removed)}");

            if (!writer.TryWrite(out string _, out string error, out bool _))
                Debug.LogError(error);
        }

        private static SolutionDotSettingsWriter WriterFor()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            return new SolutionDotSettingsWriter(projectRoot, new PackageCodeStyleTemplate().Resolve());
        }
    }
}

#endif
