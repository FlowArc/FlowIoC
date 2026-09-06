#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Root;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Reads one sub-context entry and says what state it is in.
    ///
    /// This is the half of the script reference a person meets. Before it, deleting a module left
    /// the entry looking exactly like a working one: the name stopped resolving, the sub-context was
    /// quietly not built, and the only report was an error at play time. A deleted script now reads
    /// back as null with its guid still in the scene, which is a thing the inspector can see.
    ///
    /// The lookup is a delegate so a test needs no editor. The window draws; this decides.
    /// </summary>
    internal class SubContextEntryStates
    {
        private readonly Func<string, Object> _scriptForName;

        internal SubContextEntryStates() : this(new ContextScriptResolver().ForName)
        {
        }

        internal SubContextEntryStates(Func<string, Object> scriptForName)
        {
            _scriptForName = scriptForName;
        }

        internal SubContextEntryStatus Of(SubContextData entry)
        {
            if (entry.ContextScript != null) return SubContextEntryStatus.Linked;

            return _scriptForName(entry.ContextFullName) != null
                ? SubContextEntryStatus.Unlinked
                : SubContextEntryStatus.Unresolved;
        }

        /// <summary>
        /// Whether the Resolve button does anything for this entry. Asked rather than compared, so
        /// the inspector never carries its own copy of which statuses are mendable.
        /// </summary>
        internal bool CanResolve(SubContextEntryStatus status) => status == SubContextEntryStatus.Unlinked;

        /// <summary>The script an Unlinked entry should be given, or null when there is none.</summary>
        internal Object ScriptFor(SubContextData entry) => _scriptForName(entry.ContextFullName);
    }
}

#endif
