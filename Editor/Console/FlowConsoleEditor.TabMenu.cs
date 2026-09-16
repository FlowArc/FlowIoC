#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The bar's switches a second time, as a menu: on the tab's right-click and on the ⋮ at the
    /// tab's corner, where Unity's Console keeps its line count and its stack trace setting, and
    /// on a right-click on the list where no row is. A menu has no width, so a window too narrow
    /// for the bar to hold a control still offers it - and a reader who knows Unity's Console
    /// looks here before they find the gear.
    /// </summary>
    internal partial class FlowConsoleEditor : IHasCustomMenu
    {
        /// <summary>Unity's call when the tab's menu is built, for the right-click and the ⋮ alike.</summary>
        public void AddItemsToMenu(GenericMenu menu)
        {
            AddSettingsItems(menu);
        }

        /// <summary>
        /// The bar's controls in the bar's order - Time, the line count, Flow, Pinned, then Source
        /// and Export from its right end. A dropdown becomes a submenu with the choice in force
        /// ticked, a switch ticks itself, and an item does exactly what the bar's control does,
        /// because the two call the same setter.
        /// </summary>
        internal void AddSettingsItems(GenericMenu menu)
        {
            AddTimeFormatItems(menu, "Time/");
            AddRowLinesItems(menu, "Row Lines/");
            menu.AddItem(new GUIContent("Flow"), _flowMode, () => SetFlowMode(!_flowMode));
            menu.AddItem(new GUIContent("Pinned"), _pinnedOnly, () => SetPinnedOnly(!_pinnedOnly));
            menu.AddSeparator("");
            AddSourceCaptureItems(menu, "Source/");
            AddExportItems(menu, "Export/");
        }

        /// <summary>
        /// The same menu on a right-click on the list's empty tail - under the last row, or the
        /// whole list while it is empty. A row's right-click is the row's own menu and uses the
        /// event before it gets here; a flow header takes no right-click and offers nothing.
        /// Decided in window space after the scroll view has closed, against the rect the last
        /// repaint measured, the way the wheel is.
        /// </summary>
        private void ListContextMenuGUI()
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 1 || _pointerOverStrip) return;

            Rect list = _logsViewportRect;
            float contentHeight = ContentHeight();

            // The scrollbar is the scroll view's, not the list's.
            if (contentHeight > list.height) list.width -= GUI.skin.verticalScrollbar.fixedWidth;
            if (!list.Contains(current.mousePosition)) return;

            // Where the rows end on screen, with the scroll taken off.
            float rowsEnd = list.y + contentHeight - _logsPanelScroll.y;
            if (current.mousePosition.y < rowsEnd) return;

            var menu = new GenericMenu();
            AddSettingsItems(menu);
            menu.ShowAsContext();
            current.Use();
        }
    }
}
#endif
