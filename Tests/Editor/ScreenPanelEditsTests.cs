using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.Editor.Root;
using FlowIoC.Editor.Screens;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the Screens panel does when a cell is edited. The scan that fills the panel is covered
    /// by ScreenPanelScanTests; this is the other half - the write back to the Root entry, the undo
    /// behind it, and the Reset that drops the override again.
    ///
    /// The window's edit methods are private and take a row rather than reading the GUI, so they are
    /// called directly. Every assertion is made on the Root's own entry, never on what the window
    /// happens to be showing: the window rescans the open scenes, and the scene a test runs in is
    /// whatever the Editor had open.
    /// </summary>
    public class ScreenPanelEditsTests
    {
        internal class EditsScreenView : ScreenView
        {
        }

        internal class EditsScreenMediator : IMediator
        {
            public void OnRegister()
            {
            }

            public void OnRemove()
            {
            }
        }

        internal class EditsScreenContext : ScreenSubContext<EditsScreenView, EditsScreenMediator>
        {
            protected override ScreenCVO Screen => new()
            {
                ManagerId = 0,
                Layer = 2,
                Tag = ScreenTag.GroupA,
                HasShowAnimation = true,
                HasHideAnimation = false,
                Load = ScreenLoadCVO.Addressable("EditsScreen")
            };
        }

        private readonly List<GameObject> _created = new List<GameObject>();

        private ScreenPanelScan _scan;
        private ScreenPanelWindow _window;
        private RootBase _root;

        [SetUp]
        public void SetUp()
        {
            _scan = new ScreenPanelScan(new ScreenSubContextDeclarations());
            _window = ScriptableObject.CreateInstance<ScreenPanelWindow>();

            _root = RootListing(Entry<EditsScreenContext>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_window);

            foreach (GameObject created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void A_first_edit_turns_the_override_on_and_keeps_the_declared_values_beside_it()
        {
            Write(Row(), 0, 7, ScreenTag.GroupA, true, false);

            SubContextData entry = _root.SubContextTypes[0];

            Assert.IsTrue(entry.OverrideScreen, "editing a cell must turn the Root's override on");
            Assert.AreEqual(7, entry.ScreenLayer);
            Assert.AreEqual(0, entry.ScreenManagerId);
            Assert.AreEqual(ScreenTag.GroupA, entry.ScreenTag);
            Assert.IsTrue(entry.ScreenHasShowAnimation, "the declared show animation should have come with it");
            Assert.IsFalse(entry.ScreenHasHideAnimation);
        }

        /// <summary>
        /// IsOverridden is what draws the row's asterisk and what enables its Reset button, so it is
        /// the one flag the panel's whole edited state is read from.
        /// </summary>
        [Test]
        public void An_edited_row_reads_back_as_overridden()
        {
            Write(Row(), 0, 7, ScreenTag.GroupA, true, false);

            ScreenRowEVO row = Row();

            Assert.IsTrue(row.IsOverridden);
            Assert.AreEqual(7, row.Effective.Layer);
            Assert.AreEqual(2, row.Declaration.Layer, "the declaration itself must not have moved");
        }

        [Test]
        public void Changing_the_manager_moves_the_row_to_that_manager()
        {
            Write(Row(), 3, 2, ScreenTag.GroupA, true, false);

            Assert.AreEqual(3, Row().Effective.ManagerId);
        }

        [Test]
        public void Undo_takes_the_edit_back()
        {
            Undo.IncrementCurrentGroup();

            Write(Row(), 0, 7, ScreenTag.GroupA, true, false);

            Undo.PerformUndo();

            SubContextData entry = _root.SubContextTypes[0];

            Assert.IsFalse(entry.OverrideScreen, "undo left the override on");
            Assert.AreEqual(2, Row().Effective.Layer, "undo should leave the declared layer showing");
        }

        [Test]
        public void Reset_drops_the_override_and_returns_the_declared_values()
        {
            Write(Row(), 3, 7, ScreenTag.GroupB, false, true);

            ResetToCode(Row());

            ScreenRowEVO row = Row();

            Assert.IsFalse(row.IsOverridden);
            Assert.AreEqual(0, row.Effective.ManagerId);
            Assert.AreEqual(2, row.Effective.Layer);
            Assert.AreEqual(ScreenTag.GroupA, row.Effective.Tag);
            Assert.IsTrue(row.Effective.HasShowAnimation);
        }

        /// <summary>
        /// A row addresses its entry by index, and the list can be edited in the inspector between
        /// a scan and a click. Writing then would land on whatever context took that place.
        /// </summary>
        [Test]
        public void An_entry_that_changed_since_the_scan_is_not_written()
        {
            ScreenRowEVO row = Row();

            SubContextData other = Entry<EditsScreenContext>();
            other.ContextFullName = "Some.Other.Context";
            other.ContextName = "OtherContext";
            _root.SubContextTypes[0] = other;

            Write(row, 0, 7, ScreenTag.GroupA, true, false);

            Assert.IsFalse(_root.SubContextTypes[0].OverrideScreen, "the write landed on the wrong entry");
            Assert.AreEqual("Some.Other.Context", _root.SubContextTypes[0].ContextFullName);
        }

        private ScreenRowEVO Row() => _scan.Rows(new[] {_root})[0];

        private void Write(ScreenRowEVO row, int managerId, int layer, ScreenTag tag, bool show, bool hide)
        {
            Invoke("Write", row, managerId, layer, tag, show, hide);
        }

        private void ResetToCode(ScreenRowEVO row) => Invoke("ResetToCode", row);

        private void Invoke(string name, params object[] arguments)
        {
            MethodInfo method = typeof(ScreenPanelWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

            method.Invoke(_window, arguments);
        }

        private RootBase RootListing(params SubContextData[] entries)
        {
            GameObject host = new GameObject("EditsProbeRoot");
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
    }
}
