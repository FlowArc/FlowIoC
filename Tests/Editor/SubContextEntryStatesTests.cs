using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a Root's inspector has to say about one sub-context entry.
    ///
    /// This is the half of the storage change that a person actually meets. Deleting a module used
    /// to leave the entry looking exactly like a working one - the name simply stopped resolving,
    /// and the only report was an error at play time. Now the entry either holds a live script or
    /// says which of the two ways it does not.
    /// </summary>
    public class SubContextEntryStatesTests
    {
        private class PlayerScreenContext { }

        private static Object Script() => ScriptableObject.CreateInstance<ScriptableObject>();

        private static SubContextEntryStates States(Dictionary<string, Object> byName)
        {
            return new SubContextEntryStates(
                name => byName.TryGetValue(name ?? string.Empty, out Object found) ? found : null);
        }

        [Test]
        public void An_entry_holding_a_script_is_linked()
        {
            SubContextData entry = new SubContextData
            {
                ContextScript = Script(),
                ContextFullName = typeof(PlayerScreenContext).FullName
            };

            Assert.AreEqual(SubContextEntryStatus.Linked, States(new Dictionary<string, Object>()).Of(entry));
        }

        /// <summary>
        /// An entry written before the script reference existed, or one somebody cleared. The type
        /// is still there, so one press of Resolve puts the reference back.
        /// </summary>
        [Test]
        public void An_entry_with_no_script_whose_type_still_exists_is_unlinked()
        {
            SubContextData entry = new SubContextData
            {
                ContextScript = null,
                ContextFullName = typeof(PlayerScreenContext).FullName
            };

            SubContextEntryStates states = States(
                new Dictionary<string, Object> {{typeof(PlayerScreenContext).FullName, Script()}});

            Assert.AreEqual(SubContextEntryStatus.Unlinked, states.Of(entry));
        }

        /// <summary>
        /// The case this whole change exists for: the module that declared the context has been
        /// deleted. Nothing compiles to the name, so nothing can be resolved and the Root is
        /// listing a context that will not be built.
        /// </summary>
        [Test]
        public void An_entry_naming_a_context_nothing_compiles_to_is_unresolved()
        {
            SubContextData entry = new SubContextData
            {
                ContextScript = null,
                ContextFullName = "Modules.Gone.RootsContexts.GoneContext"
            };

            Assert.AreEqual(
                SubContextEntryStatus.Unresolved, States(new Dictionary<string, Object>()).Of(entry));
        }

        [Test]
        public void An_entry_with_no_name_at_all_is_unresolved()
        {
            SubContextData entry = new SubContextData {ContextScript = null, ContextFullName = null};

            Assert.AreEqual(
                SubContextEntryStatus.Unresolved, States(new Dictionary<string, Object>()).Of(entry));
        }

        /// <summary>
        /// Only Unlinked can be repaired by the button, so the inspector asks this rather than
        /// comparing statuses itself.
        /// </summary>
        [Test]
        public void Only_an_unlinked_entry_can_be_resolved()
        {
            SubContextEntryStates states = States(new Dictionary<string, Object>());

            Assert.IsTrue(states.CanResolve(SubContextEntryStatus.Unlinked));
            Assert.IsFalse(states.CanResolve(SubContextEntryStatus.Linked));
            Assert.IsFalse(states.CanResolve(SubContextEntryStatus.Unresolved));
        }

        /// <summary>
        /// The statuses are ordered worst-last, the way ModuleCheckStatus is, so a Root listing
        /// several entries can take the worst of them by comparing.
        /// </summary>
        [Test]
        public void The_statuses_are_ordered_from_settled_to_worst()
        {
            Assert.Less((int) SubContextEntryStatus.Linked, (int) SubContextEntryStatus.Unlinked);
            Assert.Less((int) SubContextEntryStatus.Unlinked, (int) SubContextEntryStatus.Unresolved);
        }
    }
}
