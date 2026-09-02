using System;
using FlowIoC.Editor.Root;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A Root that lists half a dozen screen contexts is unreadable with every entry expanded, so
    /// the entries fold. The state is kept per Root and per context, which is what lets two Roots
    /// list the same screen context and fold it separately.
    ///
    /// Each test uses a context name of its own: the state outlives a test in the Editor session,
    /// the way it is meant to survive a selection change, so re-running the suite would otherwise
    /// see what the previous run left behind.
    /// </summary>
    public class SubContextFoldoutsTests
    {
        private const int RootId = 4321;

        private SubContextFoldouts _foldouts;
        private string _contextName;

        [SetUp]
        public void SetUp()
        {
            _foldouts = new SubContextFoldouts();
            _contextName = "Probe.Context." + Guid.NewGuid();
        }

        [Test]
        public void Both_states_start_collapsed()
        {
            Assert.IsFalse(_foldouts.IsEntryExpanded(RootId, _contextName));
            Assert.IsFalse(_foldouts.IsScreenExpanded(RootId, _contextName));
        }

        [Test]
        public void An_expanded_entry_is_remembered()
        {
            _foldouts.SetEntryExpanded(RootId, _contextName, true);

            Assert.IsTrue(_foldouts.IsEntryExpanded(RootId, _contextName));

            _foldouts.SetEntryExpanded(RootId, _contextName, false);

            Assert.IsFalse(_foldouts.IsEntryExpanded(RootId, _contextName));
        }

        [Test]
        public void The_entry_and_the_screen_fold_independently()
        {
            _foldouts.SetEntryExpanded(RootId, _contextName, true);

            Assert.IsTrue(_foldouts.IsEntryExpanded(RootId, _contextName));
            Assert.IsFalse(_foldouts.IsScreenExpanded(RootId, _contextName));

            _foldouts.SetScreenExpanded(RootId, _contextName, true);
            _foldouts.SetEntryExpanded(RootId, _contextName, false);

            Assert.IsFalse(_foldouts.IsEntryExpanded(RootId, _contextName));
            Assert.IsTrue(_foldouts.IsScreenExpanded(RootId, _contextName));
        }

        [Test]
        public void Two_roots_listing_the_same_context_fold_independently()
        {
            _foldouts.SetEntryExpanded(RootId, _contextName, true);

            Assert.IsFalse(_foldouts.IsEntryExpanded(RootId + 1, _contextName));
        }

        [Test]
        public void A_second_reader_sees_what_the_first_one_stored()
        {
            _foldouts.SetEntryExpanded(RootId, _contextName, true);

            Assert.IsTrue(new SubContextFoldouts().IsEntryExpanded(RootId, _contextName));
        }
    }
}
