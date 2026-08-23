#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="AgentRulesRemovalWatcher"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AgentRulesRemovalHook
    {
        static AgentRulesRemovalHook()
        {
            new AgentRulesRemovalWatcher().Subscribe();
        }
    }

    /// <summary>
    /// Removes the rule block when FlowIoC itself is uninstalled. registeringPackages fires
    /// before the domain reload, while this assembly is still loaded, which is the only window
    /// in which the package can clean up after itself. It does not fire when manifest.json is
    /// hand-edited or the folder is deleted; the validity sentence at the top of the block is
    /// what covers those paths.
    /// </summary>
    internal class AgentRulesRemovalWatcher
    {
        internal const string PackageName = "com.flowarc.flowioc.core";

        private readonly string _projectRoot;
        private readonly AgentRulesSource _source;

        internal AgentRulesRemovalWatcher()
            : this(new ProjectRoot().Resolve(), new AgentRulesSource())
        {
        }

        internal AgentRulesRemovalWatcher(string projectRoot, AgentRulesSource source)
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
        /// The decision, separated from Unity's event so it can be tested. Stripping the block
        /// only needs the markers, so this works even once the rule text is on its way out.
        /// </summary>
        internal void HandleRemoval(IEnumerable<string> removedPackageNames)
        {
            if (removedPackageNames == null || !removedPackageNames.Contains(PackageName))
                return;

            new AgentRulesSynchronizer(_projectRoot, _source).RemoveBlocks();
        }

        private void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            HandleRemoval(args?.removed?.Select(package => package.name));
        }
    }
}

#endif
