#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Which help boxes are open. Keyed by type rather than by instance, because what a reader
    /// opened is a fact about the component's kind: the second PlayerRoot in the scene should
    /// show what the first one was already explaining.
    ///
    /// SessionState rather than EditorPrefs - reading help is something done while working on a
    /// thing, not a setting carried between projects.
    /// </summary>
    internal class FlowHelpState
    {
        public bool IsOpen(Type type, string member)
        {
            return SessionState.GetBool(Key(type, member), false);
        }

        public void SetOpen(Type type, string member, bool open)
        {
            SessionState.SetBool(Key(type, member), open);
        }

        public void SetAll(Type type, IEnumerable<string> members, bool open)
        {
            foreach (string member in members)
                SetOpen(type, member, open);
        }

        private string Key(Type type, string member) => $"FlowIoC.Help.{type.Name}.{member}";
    }
}

#endif
