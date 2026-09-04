#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="AgentSkillsStartup"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AgentSkillsStartupHook
    {
        static AgentSkillsStartupHook()
        {
            EditorApplication.delayCall += () => new AgentSkillsStartup().Run();
        }
    }

    /// <summary>
    /// Installs the skills FlowIoC ships as soon as the Editor opens, without asking, and writes
    /// them again whenever they fall out of date. A skill is reference material for an AI
    /// assistant, not a change to the project, so a consumer of the package gets one that works
    /// out of the box rather than one that waits behind a button.
    ///
    /// It is not silent, though: what gets written is logged, so nobody has to wonder where a
    /// folder under .claude came from. Deleting an installed skill puts it back on the next
    /// Editor session. A project that would rather decide for itself turns the switch off in the
    /// Agent Scanner window, and then nothing is written until Sync is pressed.
    ///
    /// There is deliberately no session guard, for the reason the agent rules have none: whether
    /// the skills are current is answered by reading them, and a pass that has already installed
    /// finds nothing left to do - where a flag would have gone on eliding the one event that
    /// matters, since updating the package is a domain reload inside the same session and
    /// SessionState outlives one.
    /// </summary>
    internal class AgentSkillsStartup
    {
        private readonly AgentSkillsAutoSync _autoSync = new AgentSkillsAutoSync();

        internal void Run()
        {
            // A batch run has no assistant to read them and no business writing into the
            // workspace it was handed, so the install is a thing that happens for a person.
            if (Application.isBatchMode)
                return;

            string projectRoot = new ProjectRoot().Resolve();

            if (_autoSync.IsOff(projectRoot))
                return;

            AgentSkillsInstallReport report =
                new AgentSkillsAutoInstall(projectRoot, new AgentSkillsSource()).Run();

            foreach (string name in report.Installed)
                Debug.Log($"[FlowIoC] Agent skill installed: {AgentSkillsInstaller.TargetFolder}/{name}");

            foreach (string failure in report.Failures)
                Debug.LogWarning($"[FlowIoC] An agent skill could not be installed: {failure}");
        }
    }

    /// <summary>What one automatic run wrote, and what it could not.</summary>
    internal readonly struct AgentSkillsInstallReport
    {
        internal IReadOnlyList<string> Installed { get; }
        internal IReadOnlyList<string> Failures { get; }

        internal AgentSkillsInstallReport(IReadOnlyList<string> installed, IReadOnlyList<string> failures)
        {
            Installed = installed;
            Failures = failures;
        }
    }

    /// <summary>
    /// Writes whatever is missing or out of date and says what it did. Separate from the startup
    /// hook so the decision - which is the part worth being sure about - can be tested against a
    /// temporary directory instead of an Editor session.
    /// </summary>
    internal class AgentSkillsAutoInstall
    {
        private readonly string _projectRoot;
        private readonly AgentSkillsSource _source;

        internal AgentSkillsAutoInstall(string projectRoot, AgentSkillsSource source)
        {
            _projectRoot = projectRoot;
            _source = source;
        }

        internal AgentSkillsInstallReport Run()
        {
            var installer = new AgentSkillsInstaller(_projectRoot, _source);

            SyncFileState[] before = installer.Inspect();
            var pending = new List<string>();
            var failures = new List<string>();

            foreach (SyncFileState state in before)
            {
                if (state.Status == SyncStatus.Absent || state.Status == SyncStatus.Stale)
                    pending.Add(Path.GetFileName(state.Path));
                else if (state.Status == SyncStatus.Failed)
                    failures.Add(state.Message);
            }

            // The common case, every session after the first: nothing to write, nothing to say.
            if (pending.Count == 0)
                return new AgentSkillsInstallReport(Array.Empty<string>(), failures);

            var installed = new List<string>();

            foreach (SyncFileState state in installer.Install())
            {
                string name = Path.GetFileName(state.Path);

                if (state.Status == SyncStatus.Failed)
                    failures.Add(state.Message);
                else if (pending.Contains(name))
                    installed.Add(name);
            }

            return new AgentSkillsInstallReport(installed, failures);
        }
    }
}

#endif
