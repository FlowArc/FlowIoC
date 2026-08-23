#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Remembers that a consumer asked not to be prompted about the agent rules again.
    /// EditorPrefs is shared by every project a user opens with the same Editor, so the key
    /// carries the project root: declining in one project must not silence the notice in
    /// every other one. The stored value is the rule hash, so a later rule change asks again.
    /// </summary>
    internal class AgentRulesDismissal
    {
        private const string KeyPrefix = "FlowIoC.AgentRules.Dismissed.";

        internal string KeyFor(string projectRoot)
        {
            string normalized = (projectRoot ?? string.Empty)
                .Replace('\\', '/')
                .TrimEnd('/')
                .ToLowerInvariant();

            return KeyPrefix + new ManagedBlockWriter().ComputeHash(normalized);
        }

        internal bool IsDismissed(string projectRoot, string rulesHash)
        {
            return EditorPrefs.GetString(KeyFor(projectRoot), string.Empty) == rulesHash;
        }

        internal void Dismiss(string projectRoot, string rulesHash)
        {
            EditorPrefs.SetString(KeyFor(projectRoot), rulesHash);
        }

        internal void Clear(string projectRoot)
        {
            EditorPrefs.DeleteKey(KeyFor(projectRoot));
        }

        internal bool HasDismissal(string projectRoot)
        {
            return !string.IsNullOrEmpty(EditorPrefs.GetString(KeyFor(projectRoot), string.Empty));
        }
    }
}

#endif
