#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Keeps a sub-context entry's name in step with the script it points at.
    ///
    /// An entry holds two things about one context: the script asset, which is the truth, and the
    /// full name, which is the only half runtime can read - MonoScript lives in UnityEditor and a
    /// player build nulls the reference. Left alone the two drift apart the moment somebody renames
    /// the class inside its file, and the Root then lists a context that resolves to nothing.
    ///
    /// Drifted answers without changing anything, because the inspector draws on every repaint and
    /// may only mark the Root dirty when something actually moved.
    /// </summary>
    internal class SubContextNameSync
    {
        private readonly Func<Object, Type> _classOf;

        internal SubContextNameSync() : this(new ContextScriptResolver().TypeOf)
        {
        }

        internal SubContextNameSync(Func<Object, Type> classOf)
        {
            _classOf = classOf;
        }

        /// <summary>
        /// The entry with its two name fields taken from the script, and everything else left
        /// exactly as it was - the Root's screen override above all, which is the scene's answer
        /// and not the script's.
        /// </summary>
        internal SubContextData Applied(SubContextData entry)
        {
            Type type = TypeBehind(entry);

            if (type == null) return entry;

            entry.ContextFullName = type.FullName;
            entry.ContextName = type.Name;

            return entry;
        }

        /// <summary>
        /// The positions in a Root's list whose names have drifted from the scripts they point at.
        ///
        /// This is the pass the Root inspector runs before it draws, and it answers positions rather
        /// than rewriting them so the caller keeps the Undo record and the dirty mark it already has
        /// for a written entry. Nothing drifting means nothing written, which is what lets this run
        /// on every repaint.
        /// </summary>
        internal IReadOnlyList<int> DriftedIn(IReadOnlyList<SubContextData> entries)
        {
            var drifted = new List<int>();

            if (entries == null) return drifted;

            for (var index = 0; index < entries.Count; index++)
            {
                if (Drifted(entries[index])) drifted.Add(index);
            }

            return drifted;
        }

        /// <summary>Whether Applied would change this entry.</summary>
        internal bool Drifted(SubContextData entry)
        {
            Type type = TypeBehind(entry);

            if (type == null) return false;

            return entry.ContextFullName != type.FullName || entry.ContextName != type.Name;
        }

        /// <summary>
        /// The class the entry's script declares, or null when there is no script or the script
        /// declares nothing readable. A MonoScript whose file is named something other than the
        /// class answers null from GetClass, and blanking a working name on that would take the
        /// entry apart - so the script is ignored and the name stands.
        /// </summary>
        private Type TypeBehind(SubContextData entry)
        {
            return entry.ContextScript == null ? null : _classOf(entry.ContextScript);
        }
    }
}

#endif