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
    /// A Root's sub-context entry holds two things about the same context: the script asset, which
    /// is the truth, and the full name, which is the only half runtime can read - MonoScript lives
    /// in UnityEditor and a player build nulls the reference.
    ///
    /// The sync keeps the second from drifting away from the first. It is what makes a context
    /// renamed inside its file, or moved between folders, repair itself the next time somebody
    /// opens the Root rather than silently stop resolving.
    /// </summary>
    public class SubContextNameSyncTests
    {
        private class PlayerScreenContext
        {
        }

        private static Object Script() => ScriptableObject.CreateInstance<ScriptableObject>();

        private static SubContextNameSync Sync(Object script, Type behind)
        {
            var classes = new Dictionary<Object, Type>();

            if (script != null && behind != null) classes.Add(script, behind);

            return new SubContextNameSync(candidate => candidate != null && classes.TryGetValue(candidate, out Type type) ? type : null);
        }

        [Test]
        public void A_name_that_disagrees_with_the_script_is_rewritten()
        {
            Object script = Script();

            SubContextData applied = Sync(script, typeof(PlayerScreenContext)).Applied(new SubContextData
            {
                ContextScript = script,
                ContextFullName = "Modules.Player.RootsContexts.WhateverItUsedToBeCalled",
                ContextName = "WhateverItUsedToBeCalled"
            });

            Assert.AreEqual(typeof(PlayerScreenContext).FullName, applied.ContextFullName);
            Assert.AreEqual(nameof(PlayerScreenContext), applied.ContextName);
        }

        [Test]
        public void An_entry_with_no_script_keeps_the_name_it_has()
        {
            SubContextData applied = Sync(null, null).Applied(new SubContextData
            {
                ContextScript = null,
                ContextFullName = "Modules.Player.RootsContexts.PlayerScreenContext",
                ContextName = "PlayerScreenContext"
            });

            Assert.AreEqual("Modules.Player.RootsContexts.PlayerScreenContext", applied.ContextFullName);
            Assert.AreEqual("PlayerScreenContext", applied.ContextName);
        }

        /// <summary>
        /// A MonoScript whose file is named something other than the class answers null from
        /// GetClass. Blanking the name on that would take a working entry apart, so the script is
        /// ignored and the name stands.
        /// </summary>
        [Test]
        public void A_script_whose_class_cannot_be_read_leaves_the_name_alone()
        {
            SubContextData applied = Sync(Script(), null).Applied(new SubContextData
            {
                ContextScript = Script(),
                ContextFullName = "Modules.Player.RootsContexts.PlayerScreenContext",
                ContextName = "PlayerScreenContext"
            });

            Assert.AreEqual("Modules.Player.RootsContexts.PlayerScreenContext", applied.ContextFullName);
            Assert.AreEqual("PlayerScreenContext", applied.ContextName);
        }

        /// <summary>
        /// The sync only ever touches the two name fields. Everything else on the entry - the
        /// Root's screen override above all - is the scene's answer and not the script's.
        /// </summary>
        [Test]
        public void Nothing_but_the_two_name_fields_is_touched()
        {
            Object script = Script();

            SubContextData applied = Sync(script, typeof(PlayerScreenContext)).Applied(new SubContextData
            {
                ContextScript = script,
                ContextFullName = "stale",
                ContextName = "stale",
                AutoSetup = true,
                IsTest = true,
                OverrideScreen = true,
                ScreenManagerId = 3,
                ScreenLayer = 7,
                ScreenHasShowAnimation = true,
                ScreenHasHideAnimation = true
            });

            Assert.IsTrue(applied.AutoSetup);
            Assert.IsTrue(applied.IsTest);
            Assert.IsTrue(applied.OverrideScreen);
            Assert.AreEqual(3, applied.ScreenManagerId);
            Assert.AreEqual(7, applied.ScreenLayer);
            Assert.IsTrue(applied.ScreenHasShowAnimation);
            Assert.IsTrue(applied.ScreenHasHideAnimation);
            Assert.AreSame(script, applied.ContextScript);
        }

        [Test]
        public void A_name_that_already_agrees_with_the_script_is_reported_as_unchanged()
        {
            Object script = Script();

            var entry = new SubContextData
            {
                ContextScript = script,
                ContextFullName = typeof(PlayerScreenContext).FullName,
                ContextName = nameof(PlayerScreenContext)
            };

            Assert.IsFalse(Sync(script, typeof(PlayerScreenContext)).Drifted(entry));
        }

        /// <summary>
        /// The inspector draws on every repaint and may only mark the Root dirty when something
        /// actually moved, so the sync has to be able to say whether it would change anything
        /// without being asked to change it.
        /// </summary>
        [Test]
        public void A_name_that_disagrees_with_the_script_is_reported_as_drifted()
        {
            Object script = Script();

            var entry = new SubContextData
            {
                ContextScript = script,
                ContextFullName = "Modules.Player.RootsContexts.WhateverItUsedToBeCalled",
                ContextName = "WhateverItUsedToBeCalled"
            };

            Assert.IsTrue(Sync(script, typeof(PlayerScreenContext)).Drifted(entry));
        }

        [Test]
        public void An_entry_with_no_script_never_counts_as_drifted()
        {
            var entry = new SubContextData
            {
                ContextScript = null,
                ContextFullName = "Modules.Player.RootsContexts.PlayerScreenContext",
                ContextName = "PlayerScreenContext"
            };

            Assert.IsFalse(Sync(null, null).Drifted(entry));
        }

        /// <summary>
        /// The pass the Root inspector runs before it draws. It answers positions rather than
        /// rewriting them, so the caller keeps the Undo record it already has for a written entry -
        /// and so a list where nothing has moved costs no write at all, which is what lets this run
        /// on every repaint.
        /// </summary>
        [Test]
        public void A_list_where_nothing_moved_reports_no_positions()
        {
            Object script = Script();

            var entries = new List<SubContextData>
            {
                new()
                {
                    ContextScript = script,
                    ContextFullName = typeof(PlayerScreenContext).FullName,
                    ContextName = nameof(PlayerScreenContext)
                }
            };

            Assert.IsEmpty(Sync(script, typeof(PlayerScreenContext)).DriftedIn(entries));
        }

        [Test]
        public void The_position_of_a_renamed_context_is_reported()
        {
            Object renamed = Script();

            var entries = new List<SubContextData>
            {
                new()
                {
                    ContextScript = null,
                    ContextFullName = "Modules.Player.RootsContexts.SomethingElse",
                    ContextName = "SomethingElse"
                },
                new()
                {
                    ContextScript = renamed,
                    ContextFullName = "Modules.Player.RootsContexts.WhateverItUsedToBeCalled",
                    ContextName = "WhateverItUsedToBeCalled"
                }
            };

            CollectionAssert.AreEqual(
                new[] {1}, Sync(renamed, typeof(PlayerScreenContext)).DriftedIn(entries));
        }

        /// <summary>
        /// A rename leaves the entry Linked - the script reference follows the file - so nothing is
        /// drawn under it and this pass is the only thing that notices. Applied at the reported
        /// position is what the inspector then writes back.
        /// </summary>
        [Test]
        public void Applying_at_the_reported_position_brings_the_name_back()
        {
            Object renamed = Script();
            SubContextNameSync sync = Sync(renamed, typeof(PlayerScreenContext));

            var entries = new List<SubContextData>
            {
                new()
                {
                    ContextScript = renamed,
                    ContextFullName = "Modules.Player.RootsContexts.WhateverItUsedToBeCalled",
                    ContextName = "WhateverItUsedToBeCalled"
                }
            };

            foreach (int index in sync.DriftedIn(entries))
                entries[index] = sync.Applied(entries[index]);

            Assert.AreEqual(typeof(PlayerScreenContext).FullName, entries[0].ContextFullName);
            Assert.AreEqual(nameof(PlayerScreenContext), entries[0].ContextName);
            Assert.IsEmpty(sync.DriftedIn(entries));
        }

        [Test]
        public void A_list_that_is_null_reports_no_positions()
        {
            Assert.IsEmpty(Sync(null, null).DriftedIn(null));
        }
    }
}