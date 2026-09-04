using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.Editor.Root;
using FlowIoC.Editor.ScreenScanner;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The panel lists what the Roots in the open scenes carry. The scan is handed its Roots
    /// rather than finding them, so these tests describe exactly the Roots they build instead of
    /// whatever scene happens to be open.
    /// </summary>
    public class ScreenScannerRunnerTests
    {
        internal class PanelScreenView : ScreenView
        {
        }

        internal class PanelScreenMediator : IMediator
        {
            public void OnRegister()
            {
            }

            public void OnRemove()
            {
            }
        }

        internal class PanelScreenContext : ScreenSubContext<PanelScreenView, PanelScreenMediator>
        {
            protected override ScreenCVO Screen => new()
            {
                ManagerId = 0,
                Layer = 2,
                Tag = ScreenTag.GroupA,
                HasShowAnimation = true,
                Load = ScreenLoadCVO.Addressable("PanelScreen")
            };
        }

        internal class BrokenPanelScreenContext : ScreenSubContext<PanelScreenView, PanelScreenMediator>
        {
            protected override ScreenCVO Screen => throw new InvalidOperationException("not readable");
        }

        private readonly List<GameObject> _created = new();

        private ScreenScannerRunner _scan;

        [SetUp]
        public void SetUp() => _scan = new ScreenScannerRunner(new ScreenSubContextDeclarations());

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        private RootBase RootListing(params SubContextData[] entries)
        {
            GameObject host = new GameObject("PanelProbeRoot");
            _created.Add(host);

            // ScreenServiceRoot is a concrete Root the package ships, and edit mode does not run
            // its Awake, so it is a plain RootBase with a SubContextTypes list here.
            RootBase root = host.AddComponent<ScreenServiceRoot>();
            root.SubContextTypes = new List<SubContextData>(entries);
            return root;
        }

        private static SubContextData Entry<TContext>()
        {
            return new SubContextData
            {
                ContextFullName = typeof(TContext).FullName,
                ContextName = typeof(TContext).Name,
                AutoSetup = true
            };
        }

        private static SubContextData OverridingEntry<TContext>(int managerId, int layer)
        {
            SubContextData data = Entry<TContext>();
            data.OverrideScreen = true;
            data.ScreenManagerId = managerId;
            data.ScreenLayer = layer;
            data.ScreenTag = ScreenTag.GroupB;
            data.ScreenHasHideAnimation = true;
            return data;
        }

        [Test]
        public void A_screen_context_becomes_one_row_that_knows_where_it_came_from()
        {
            RootBase root = RootListing(Entry<PanelScreenContext>());

            List<ScreenRowEVO> rows = _scan.Rows(new[] {root});

            Assert.AreEqual(1, rows.Count);
            Assert.AreSame(root, rows[0].Root);
            Assert.AreEqual(0, rows[0].EntryIndex);
            Assert.AreEqual(nameof(PanelScreenContext), rows[0].ContextName);
        }

        [Test]
        public void A_context_that_is_not_a_screen_produces_no_row()
        {
            RootBase root = RootListing(new SubContextData
            {
                ContextFullName = typeof(ScreenScannerRunnerTests).FullName,
                ContextName = nameof(ScreenScannerRunnerTests)
            });

            Assert.AreEqual(0, _scan.Rows(new[] {root}).Count);
        }

        [Test]
        public void An_entry_with_no_override_shows_what_the_context_declares()
        {
            RootBase root = RootListing(Entry<PanelScreenContext>());

            ScreenRowEVO row = _scan.Rows(new[] {root})[0];

            Assert.IsFalse(row.IsOverridden);
            Assert.AreEqual(0, row.Effective.ManagerId);
            Assert.AreEqual(2, row.Effective.Layer);
            Assert.AreEqual(ScreenTag.GroupA, row.Effective.Tag);
            Assert.IsTrue(row.Effective.HasShowAnimation);
            Assert.AreEqual("PanelScreen", row.Effective.Load.Key);
        }

        [Test]
        public void An_overriding_entry_shows_the_root_values_and_the_declared_load()
        {
            RootBase root = RootListing(OverridingEntry<PanelScreenContext>(1, 5));

            ScreenRowEVO row = _scan.Rows(new[] {root})[0];

            Assert.IsTrue(row.IsOverridden);
            Assert.AreEqual(1, row.Effective.ManagerId);
            Assert.AreEqual(5, row.Effective.Layer);
            Assert.AreEqual(ScreenTag.GroupB, row.Effective.Tag);
            Assert.IsFalse(row.Effective.HasShowAnimation);
            Assert.IsTrue(row.Effective.HasHideAnimation);
            Assert.AreEqual("PanelScreen", row.Effective.Load.Key);
        }

        [Test]
        public void A_declaration_that_cannot_be_read_leaves_the_row_without_values()
        {
            RootBase root = RootListing(Entry<BrokenPanelScreenContext>());

            ScreenRowEVO row = _scan.Rows(new[] {root})[0];

            Assert.IsNull(row.Declaration);
            Assert.IsNull(row.Effective);
            Assert.IsTrue(row.DeclarationError.Contains(nameof(BrokenPanelScreenContext)));
        }

        [Test]
        public void An_unreadable_declaration_still_shows_what_the_root_overrode()
        {
            RootBase root = RootListing(OverridingEntry<BrokenPanelScreenContext>(1, 4));

            ScreenRowEVO row = _scan.Rows(new[] {root})[0];

            Assert.IsNull(row.Declaration);
            Assert.IsNotNull(row.Effective);
            Assert.AreEqual(1, row.Effective.ManagerId);
            Assert.AreEqual(4, row.Effective.Layer);
            Assert.IsFalse(row.Effective.Load.IsValid);
        }

        [Test]
        public void Every_entry_on_every_root_is_listed_with_its_own_index()
        {
            RootBase first = RootListing(Entry<PanelScreenContext>(), OverridingEntry<BrokenPanelScreenContext>(1, 1));
            RootBase second = RootListing(Entry<PanelScreenContext>());

            List<ScreenRowEVO> rows = _scan.Rows(new[] {first, second});

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual(0, rows[0].EntryIndex);
            Assert.AreEqual(1, rows[1].EntryIndex);
            Assert.AreSame(second, rows[2].Root);
        }

        [Test]
        public void A_root_with_no_sub_contexts_is_skipped_rather_than_throwing()
        {
            GameObject bare = new GameObject("BareRoot");
            _created.Add(bare);
            RootBase root = bare.AddComponent<ScreenServiceRoot>();
            root.SubContextTypes = null;

            Assert.AreEqual(0, _scan.Rows(new[] {root}).Count);
        }
    }
}
