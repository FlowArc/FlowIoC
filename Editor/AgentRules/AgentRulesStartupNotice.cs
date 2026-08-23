#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="AgentRulesStartupNotice"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AgentRulesStartupHook
    {
        static AgentRulesStartupHook()
        {
            EditorApplication.delayCall += () => new AgentRulesStartupNotice().Run();
        }
    }

    /// <summary>
    /// Offers to install or refresh the rule block once per session, and only when it is
    /// actually absent or stale. A declined offer is remembered against the rule hash, so a
    /// consumer who says no is not asked again until the rules themselves change.
    /// </summary>
    internal class AgentRulesStartupNotice
    {
        private const string SessionKey = "FlowIoC.AgentRules.Checked";
        private const string DismissedKey = "FlowIoC.AgentRules.DismissedHash";

        internal void Run()
        {
            // A modal dialog during a headless domain reload would block the run forever.
            if (Application.isBatchMode)
                return;

            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);

            var source = new AgentRulesSource();
            if (!source.TryRead(out string rules, out _))
                return;

            string hash = new ManagedBlockWriter().ComputeHash(rules);
            if (EditorPrefs.GetString(DismissedKey, string.Empty) == hash)
                return;

            string projectRoot = new ProjectRoot().Resolve();
            var states = new AgentRulesSynchronizer(projectRoot, source).Inspect();

            if (!states.Any(s => s.Status == SyncStatus.Absent || s.Status == SyncStatus.Stale))
                return;

            bool absent = states.Any(s => s.Status == SyncStatus.Absent);

            int choice = EditorUtility.DisplayDialogComplex(
                "FlowIoC agent rules",
                absent
                    ? "FlowIoC can write its architecture rules into this project's AGENTS.md so AI "
                      + "coding assistants follow them. Rules you wrote yourself are left untouched.\n\n"
                      + "Install them now?"
                    : "FlowIoC's architecture rules have changed since they were written into this "
                      + "project's AGENTS.md.\n\nUpdate them now?",
                "Sync now",
                "Later",
                "Do not ask again");

            switch (choice)
            {
                case 0:
                    new AgentRulesSynchronizer(projectRoot, source).Sync();
                    AssetDatabase.Refresh();
                    break;

                case 2:
                    EditorPrefs.SetString(DismissedKey, hash);
                    break;
            }
        }
    }
}

#endif
