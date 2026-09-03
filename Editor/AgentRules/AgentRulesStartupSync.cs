#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="AgentRulesStartupSync"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AgentRulesStartupHook
    {
        static AgentRulesStartupHook()
        {
            EditorApplication.delayCall += () => new AgentRulesStartupSync().Run();
        }
    }

    /// <summary>
    /// Keeps the rule block in AGENTS.md and CLAUDE.md describing the version of FlowIoC the
    /// project is on, writing it whenever it is absent or stale.
    ///
    /// It used to ask first, with a modal dialog. That was wrong twice over. A modal on startup
    /// blocks every other load callback behind it - the setup modules install from one - and the
    /// question it asked had one sensible answer, because a stale block describes a version of
    /// FlowIoC the project is no longer on and helps nobody. The block is generated text between
    /// two markers, so nothing a reader wrote is ever touched, which is what makes writing it
    /// without asking honest. A project that wants none of it says so once in the Agent Rules
    /// window.
    ///
    /// There is deliberately no session guard. Whether the files are current is answered by
    /// reading them, and a check that has already synced finds nothing left to do - where a flag
    /// would have gone on eliding the one event that matters, since updating the package is a
    /// domain reload inside the same session and SessionState outlives one.
    /// </summary>
    internal class AgentRulesStartupSync
    {
        private readonly AgentRulesAutoSync _autoSync = new AgentRulesAutoSync();

        internal void Run()
        {
            // A batch run has no business writing into the workspace it was handed, and nobody to
            // read what it wrote. An interactive session on the same project still gets its turn.
            if (Application.isBatchMode)
                return;

            string projectRoot = new ProjectRoot().Resolve();

            if (_autoSync.IsOff(projectRoot))
                return;

            var source = new AgentRulesSource();
            if (!source.TryRead(out _, out _))
                return;

            var synchronizer = new AgentRulesSynchronizer(projectRoot, source);
            SyncFileState[] before = synchronizer.Inspect();

            if (!before.Any(NeedsWriting))
                return;

            // Only the files that were actually going to be written are reported. Sync answers
            // Current for a file it left alone as well as for one it wrote, so without this a
            // CLAUDE.md that was already up to date would announce itself every time AGENTS.md
            // needed a line.
            var written = new HashSet<string>(before.Where(NeedsWriting).Select(state => state.Path));

            foreach (SyncFileState state in synchronizer.Sync())
            {
                if (written.Contains(state.Path))
                    Report(state);
            }
        }

        private bool NeedsWriting(SyncFileState state)
        {
            return state.Status == SyncStatus.Absent || state.Status == SyncStatus.Stale;
        }

        /// <summary>
        /// One line per file, the way the agent skills report themselves. A file the writer could
        /// not take - one whose markers a reader has broken, say - says so rather than passing in
        /// silence, because from here on nobody is watching a dialog for it.
        /// </summary>
        private void Report(SyncFileState state)
        {
            string name = Path.GetFileName(state.Path);

            if (state.Status == SyncStatus.Current)
            {
                Debug.Log($"<color=cyan>[FlowIoC]</color> Agent rules written: {name}");
                return;
            }

            Debug.LogWarning(
                $"<color=cyan>[FlowIoC]</color> Agent rules could not be written into {name}: "
                + $"{state.Status}. {state.Message}");
        }
    }
}

#endif