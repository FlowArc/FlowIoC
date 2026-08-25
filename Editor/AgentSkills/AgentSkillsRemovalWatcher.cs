#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="AgentSkillsRemovalWatcher"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AgentSkillsRemovalHook
    {
        static AgentSkillsRemovalHook()
        {
            new AgentSkillsRemovalWatcher().Subscribe();
        }
    }

    /// <summary>
    /// Takes the shipped skills back out when FlowIoC itself is uninstalled. Nobody asked for
    /// them in the first place - they are written automatically when the Editor opens - so
    /// leaving them behind would leave a consumer with folders they never chose and can no
    /// longer explain.
    ///
    /// registeringPackages fires before the domain reload, while this assembly is still loaded
    /// and the package is still on disk, which is the only window in which the file list can be
    /// read and the package can clean up after itself. It does not fire when manifest.json is
    /// hand-edited or the folder is deleted by hand; those leave the skills in place, and the
    /// consumer can delete the folder.
    /// </summary>
    internal class AgentSkillsRemovalWatcher
    {
        internal const string PackageName = "com.flowarc.flowioc.core";

        private readonly string _projectRoot;
        private readonly AgentSkillsSource _source;

        internal AgentSkillsRemovalWatcher()
            : this(new ProjectRoot().Resolve(), new AgentSkillsSource())
        {
        }

        internal AgentSkillsRemovalWatcher(string projectRoot, AgentSkillsSource source)
        {
            _projectRoot = projectRoot;
            _source = source;
        }

        internal void Subscribe()
        {
            Events.registeringPackages -= OnRegisteringPackages;
            Events.registeringPackages += OnRegisteringPackages;
        }

        /// <summary>
        /// The decision, separated from Unity's event so it can be tested. What was removed is
        /// reported for the same reason the install is logged: a folder appearing or vanishing
        /// under .claude should never be a mystery.
        /// </summary>
        internal SyncFileState[] HandleRemoval(IEnumerable<string> removedPackageNames)
        {
            if (removedPackageNames == null || !removedPackageNames.Contains(PackageName))
                return new SyncFileState[0];

            return new AgentSkillsInstaller(_projectRoot, _source).Uninstall();
        }

        private void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            foreach (SyncFileState state in HandleRemoval(args?.removed?.Select(package => package.name)))
            {
                if (state.Status == SyncStatus.Failed)
                    Debug.LogWarning($"[FlowIoC] An agent skill could not be removed: {state.Message}");
                else
                    Debug.Log($"[FlowIoC] Agent skill removed: {AgentSkillsInstaller.TargetFolder}/{Path.GetFileName(state.Path)}");
            }
        }
    }
}

#endif
