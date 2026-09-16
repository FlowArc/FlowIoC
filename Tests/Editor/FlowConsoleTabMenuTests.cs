using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The tab's menu carries the bar's switches, so a bar too narrow to hold one never takes it
    /// away, and a reader who knows Unity's Console finds them where its line count is. The items
    /// are read back out of the GenericMenu by reflection, because Unity offers no way to list
    /// them, and the message on the assertion is what says so on the day an upgrade moves them.
    /// </summary>
    public class FlowConsoleTabMenuTests
    {
        private FlowConsoleEditor _window;
        private FlowConsoleState _state;
        private int _rowLineCount;
        private bool _flowMode;

        [SetUp]
        public void Open()
        {
            _state = new FlowConsoleState();
            _rowLineCount = _state.RowLineCount;
            _flowMode = _state.FlowMode;

            // Read on enable, so they are written before the window is made.
            _state.RowLineCount = 3;
            _state.FlowMode = false;

            _window = ScriptableObject.CreateInstance<FlowConsoleEditor>();
        }

        [TearDown]
        public void Close()
        {
            Object.DestroyImmediate(_window);
            _state.RowLineCount = _rowLineCount;
            _state.FlowMode = _flowMode;
        }

        [Test]
        public void The_tab_menu_offers_every_control_the_bar_can_drop()
        {
            string[] paths = Items(TabMenu()).Where(item => !item.Separator).Select(item => item.Path).ToArray();

            // The bar's order: what shapes the rows, then what sits at its right end.
            Assert.AreEqual(3, paths.Count(path => path.StartsWith("Time/")), "the three time formats");
            Assert.AreEqual(3, paths.Count(path => path.StartsWith("Row Lines/")), "one to three lines");
            Assert.Contains("Flow", paths);
            Assert.Contains("Pinned", paths);
            Assert.AreEqual(3, paths.Count(path => path.StartsWith("Source/")), "the three captures");
            Assert.AreEqual(3, paths.Count(path => path.StartsWith("Export/")), "save, save with traces, copy");

            Assert.Less(Array.IndexOf(paths, "Flow"), Array.IndexOf(paths, "Pinned"));
            Assert.Less(Array.IndexOf(paths, "Pinned"), Array.FindIndex(paths, path => path.StartsWith("Source/")));
        }

        [Test]
        public void The_line_count_in_force_is_ticked_and_picking_another_moves_the_tick()
        {
            Assert.IsTrue(Item("Row Lines/3 lines").On);
            Assert.IsFalse(Item("Row Lines/1 line").On);

            Item("Row Lines/1 line").Pick();

            Assert.AreEqual(1, new FlowConsoleState().RowLineCount, "the pick is kept the way the bar's is");
            Assert.IsTrue(Item("Row Lines/1 line").On);
            Assert.IsFalse(Item("Row Lines/3 lines").On);
        }

        [Test]
        public void A_switch_ticks_itself_and_flips_when_picked()
        {
            Assert.IsFalse(Item("Flow").On);

            Item("Flow").Pick();

            Assert.IsTrue(Item("Flow").On);
            Assert.IsTrue(new FlowConsoleState().FlowMode, "the pick is kept the way the bar's is");
        }

        private GenericMenu TabMenu()
        {
            var menu = new GenericMenu();
            ((IHasCustomMenu) _window).AddItemsToMenu(menu);
            return menu;
        }

        private MenuEntry Item(string path)
        {
            return Items(TabMenu()).Single(item => item.Path == path);
        }

        private readonly struct MenuEntry
        {
            public readonly string Path;
            public readonly bool On;
            public readonly bool Separator;
            private readonly GenericMenu.MenuFunction _pick;

            public MenuEntry(string path, bool on, bool separator, GenericMenu.MenuFunction pick)
            {
                Path = path;
                On = on;
                Separator = separator;
                _pick = pick;
            }

            public void Pick()
            {
                Assert.IsNotNull(_pick, Path + " has nothing to run");
                _pick();
            }
        }

        private static List<MenuEntry> Items(GenericMenu menu)
        {
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var list = typeof(GenericMenu).GetProperty("menuItems", any)?.GetValue(menu) as IEnumerable;
            Assert.IsNotNull(list, "GenericMenu no longer lists its items under menuItems");

            var items = new List<MenuEntry>();

            foreach (object item in list)
            {
                Type type = item.GetType();
                FieldInfo content = type.GetField("content", any);
                FieldInfo on = type.GetField("on", any);
                FieldInfo separator = type.GetField("separator", any);
                FieldInfo func = type.GetField("func", any);

                Assert.IsTrue(content != null && on != null && separator != null && func != null,
                    "GenericMenu.MenuItem no longer carries content, on, separator and func");

                items.Add(new MenuEntry(
                    ((GUIContent) content.GetValue(item)).text,
                    (bool) on.GetValue(item),
                    (bool) separator.GetValue(item),
                    func.GetValue(item) as GenericMenu.MenuFunction));
            }

            return items;
        }
    }
}
