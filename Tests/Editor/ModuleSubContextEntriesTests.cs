using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Which of a Root's sub-context entries belong to the module about to be deleted.
    ///
    /// This is what Delete Module takes out of the Roots before the folder goes, so it has to be
    /// exact in both directions: leaving one behind means a Root listing a context that no longer
    /// exists, and taking one too many silently unwires a module nobody asked about.
    /// </summary>
    public class ModuleSubContextEntriesTests
    {
        private const string MODULE = "Assets/Modules/PlayerModule";
        private const string INSIDE = "Assets/Modules/PlayerModule/Scripts/Runtime/RootsContexts/PlayerContext.cs";
        private const string OUTSIDE = "Assets/Modules/HudModule/Scripts/Runtime/RootsContexts/HudContext.cs";

        private static Object Script() => ScriptableObject.CreateInstance<ScriptableObject>();

        private static ModuleSubContextEntries Entries(
            Dictionary<Object, string> scriptPaths, Dictionary<string, string> pathsByName = null)
        {
            return new ModuleSubContextEntries(
                script => script != null && scriptPaths.TryGetValue(script, out string path) ? path : null,
                name => pathsByName != null && name != null && pathsByName.TryGetValue(name, out string p) ? p : null);
        }

        private static SubContextData Entry(Object script, string fullName = null)
        {
            return new SubContextData {ContextScript = script, ContextFullName = fullName};
        }

        [Test]
        public void An_entry_whose_script_sits_in_the_module_is_listed()
        {
            Object script = Script();

            IReadOnlyList<int> found = Entries(new Dictionary<Object, string> {{script, INSIDE}})
                .IndexesIn(new List<SubContextData> {Entry(script)}, MODULE);

            Assert.AreEqual(new[] {0}, found);
        }

        [Test]
        public void An_entry_whose_script_sits_elsewhere_is_left_alone()
        {
            Object script = Script();

            IReadOnlyList<int> found = Entries(new Dictionary<Object, string> {{script, OUTSIDE}})
                .IndexesIn(new List<SubContextData> {Entry(script)}, MODULE);

            Assert.IsEmpty(found);
        }

        /// <summary>
        /// An entry authored before the script reference. The name still names a type, and that
        /// type's script is what says which module it belongs to.
        /// </summary>
        [Test]
        public void An_entry_with_no_script_is_placed_by_the_type_its_name_resolves_to()
        {
            IReadOnlyList<int> found = Entries(
                    new Dictionary<Object, string>(),
                    new Dictionary<string, string> {{"Modules.Player.RootsContexts.PlayerContext", INSIDE}})
                .IndexesIn(
                    new List<SubContextData> {Entry(null, "Modules.Player.RootsContexts.PlayerContext")},
                    MODULE);

            Assert.AreEqual(new[] {0}, found);
        }

        /// <summary>
        /// A name that resolves to nothing is already broken by some earlier deletion, and there is
        /// no way to tell whose it was. Removing it on a guess would take out an entry that belongs
        /// to a module nobody is deleting, so it stays and the report names it.
        /// </summary>
        [Test]
        public void An_entry_that_resolves_to_nothing_at_all_is_left_alone()
        {
            IReadOnlyList<int> found = Entries(new Dictionary<Object, string>())
                .IndexesIn(new List<SubContextData> {Entry(null, "Modules.Gone.RootsContexts.GoneContext")}, MODULE);

            Assert.IsEmpty(found);
        }

        [Test]
        public void A_module_whose_path_merely_starts_the_same_is_a_different_module()
        {
            Object script = Script();
            const string neighbour = "Assets/Modules/PlayerHudModule/Scripts/Runtime/RootsContexts/HudContext.cs";

            IReadOnlyList<int> found = Entries(new Dictionary<Object, string> {{script, neighbour}})
                .IndexesIn(new List<SubContextData> {Entry(script)}, MODULE);

            Assert.IsEmpty(found);
        }

        [Test]
        public void Several_entries_come_back_in_the_order_they_are_listed()
        {
            Object mine = Script();
            Object theirs = Script();
            Object alsoMine = Script();

            IReadOnlyList<int> found = Entries(
                    new Dictionary<Object, string> {{mine, INSIDE}, {theirs, OUTSIDE}, {alsoMine, INSIDE}})
                .IndexesIn(new List<SubContextData> {Entry(mine), Entry(theirs), Entry(alsoMine)}, MODULE);

            Assert.AreEqual(new[] {0, 2}, found);
        }

        [Test]
        public void A_Root_listing_nothing_answers_with_nothing()
        {
            ModuleSubContextEntries entries = Entries(new Dictionary<Object, string>());

            Assert.IsEmpty(entries.IndexesIn(new List<SubContextData>(), MODULE));
            Assert.IsEmpty(entries.IndexesIn(null, MODULE));
        }

        [Test]
        public void An_empty_module_path_matches_nothing()
        {
            Object script = Script();

            IReadOnlyList<int> found = Entries(new Dictionary<Object, string> {{script, INSIDE}})
                .IndexesIn(new List<SubContextData> {Entry(script)}, string.Empty);

            Assert.IsEmpty(found);
        }

        /// <summary>
        /// Delete Module is driven from a window on Windows, where the module path arrives with
        /// backslashes while the asset database speaks forward slashes.
        /// </summary>
        [Test]
        public void A_module_path_with_backslashes_matches_the_same_entries()
        {
            Object script = Script();

            IReadOnlyList<int> found = Entries(new Dictionary<Object, string> {{script, INSIDE}})
                .IndexesIn(new List<SubContextData> {Entry(script)}, @"Assets\Modules\PlayerModule");

            Assert.AreEqual(new[] {0}, found);
        }

        /// <summary>
        /// Removing by index has to walk backwards or the second removal takes the wrong entry.
        /// That is the whole reason this is a method rather than a loop at the call site.
        /// </summary>
        [Test]
        public void Removing_two_entries_takes_the_two_that_were_named()
        {
            Object mine = Script();
            Object theirs = Script();
            Object alsoMine = Script();

            var list = new List<SubContextData>
            {
                Entry(mine, "A"), Entry(theirs, "B"), Entry(alsoMine, "C")
            };

            new ModuleSubContextEntries(null, null).RemoveAt(list, new List<int> {0, 2});

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("B", list[0].ContextFullName);
        }

        [Test]
        public void Removing_nothing_leaves_the_list_as_it_was()
        {
            var list = new List<SubContextData> {Entry(null, "A")};

            new ModuleSubContextEntries(null, null).RemoveAt(list, new List<int>());

            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void Removing_from_a_list_that_is_not_there_does_not_throw()
        {
            Assert.DoesNotThrow(() => new ModuleSubContextEntries(null, null).RemoveAt(null, new List<int> {0}));
        }
    }
}
