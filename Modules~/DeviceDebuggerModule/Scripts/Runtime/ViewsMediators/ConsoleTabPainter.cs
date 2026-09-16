using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The Console page, laid out the way the Flow Console lays out a two-line row: a severity
    /// icon, the message alone on the first line, the channel tag in its colour and the source
    /// under it. The kind filters and the search sit above the list; the Filters button folds
    /// out the channel switches - every channel the logger knows, grouped into the game's and the
    /// framework's. A tapped row opens its detail over the list, with a Back button, because a
    /// pane under a long list on a phone is where nobody looks. Owned by the view; it raises what
    /// the tester did and paints what it is handed, and decides nothing.
    /// </summary>
    public class ConsoleTabPainter
    {
        private const string FILTER_ON_CLASS = "dd-filter-on";
        private const string CHANNEL_ON_CLASS = "dd-channel-on";

        private readonly LogRowTextRule _rowText = new();
        private readonly Button _logs;
        private readonly Button _warnings;
        private readonly Button _errors;
        private readonly Button _filters;
        private readonly TextField _search;
        private readonly VisualElement _channelPanel;
        private readonly VisualElement _channelRows;
        private readonly ListView _list;
        private readonly VisualElement _detail;
        private readonly Label _detailHeading;
        private readonly Label _detailText;
        private readonly List<ConsoleLog> _rows = new();
        private readonly Dictionary<string, Button> _channelButtons = new();
        private bool _channelsOpen;
        private ConsoleLog _detailRow;
        private Vector2 _pressedAt;
        private float _pressedTime;

        public LogFilterVO Filter { get; } = new();

        public event Action OnFilterChanged;
        public event Action OnClearTapped;
        public event Action OnCopyTapped;
        public event Action<ConsoleLog> OnRowSelected;
        public event Action<ConsoleLog> OnCopyDetailTapped;

        public ConsoleTabPainter(VisualElement page)
        {
            _logs = page.Q<Button>("filter-logs");
            _warnings = page.Q<Button>("filter-warnings");
            _errors = page.Q<Button>("filter-errors");
            _filters = page.Q<Button>("filter-channels");
            _search = page.Q<TextField>("search");
            _channelPanel = page.Q<VisualElement>("channel-panel");
            _channelRows = page.Q<VisualElement>("channel-rows");
            _list = page.Q<ListView>("log-list");
            _detail = page.Q<VisualElement>("detail");
            _detailHeading = page.Q<Label>("detail-heading");
            _detailText = page.Q<Label>("detail-text");

            _logs.clicked += () => ToggleFilter(ref Filter.ShowLogs, _logs);
            _warnings.clicked += () => ToggleFilter(ref Filter.ShowWarnings, _warnings);
            _errors.clicked += () => ToggleFilter(ref Filter.ShowErrors, _errors);
            _filters.clicked += ToggleChannelPanel;
            _search.RegisterValueChangedCallback(changed =>
            {
                Filter.Search = changed.newValue ?? "";
                OnFilterChanged?.Invoke();
            });
            page.Q<Button>("clear").clicked += () => OnClearTapped?.Invoke();
            page.Q<Button>("copy-logs").clicked += () => OnCopyTapped?.Invoke();
            page.Q<Button>("detail-back").clicked += CloseDetail;
            page.Q<Button>("detail-copy").clicked += () => { if (_detailRow != null) OnCopyDetailTapped?.Invoke(_detailRow); };
            page.Q<Button>("channels-all").clicked += () => SetAllChannels(true);
            page.Q<Button>("channels-none").clicked += () => SetAllChannels(false);

            _list.itemsSource = _rows;
            _list.makeItem = MakeRow;
            _list.bindItem = BindRow;
            // No selection: a finger dragging the list to scroll is not a tap, and the ListView's own
            // selection took a drag for one. A row opens on a press and a release that stayed put.
            _list.selectionType = SelectionType.None;

            _channelPanel.style.display = DisplayStyle.None;
            _detail.style.display = DisplayStyle.None;
        }

        // ======================== Rows ========================

        public void PaintRows(IReadOnlyList<ConsoleLog> rows, bool keepScrolledToEnd)
        {
            _rows.Clear();
            _rows.AddRange(rows);
            _list.RefreshItems();

            // Not while a row is being read: a new line arriving would yank the list to its end.
            if (keepScrolledToEnd && _detailRow == null && _rows.Count > 0)
                _list.ScrollToItem(_rows.Count - 1);
        }

        public void PaintCounts(int logs, int warnings, int errors)
        {
            _logs.text = "Log " + logs;
            _warnings.text = "Warn " + warnings;
            _errors.text = "Error " + errors;
        }

        /// <summary>The channel switches, one per channel the logger knows; the game's first, the framework's after.</summary>
        public void PaintChannels(IReadOnlyList<FlowLogChannel> channels)
        {
            _channelRows.Clear();
            _channelButtons.Clear();

            if (channels == null) return;

            AddChannelGroup("Modules", channels, false);
            AddChannelGroup("FlowIoC", channels, true);
        }

        private void AddChannelGroup(string title, IReadOnlyList<FlowLogChannel> channels, bool framework)
        {
            var heading = new Label(title);
            heading.AddToClassList("dd-channel-heading");
            var wrap = new VisualElement();
            wrap.AddToClassList("dd-channel-wrap");
            int count = 0;

            foreach (FlowLogChannel channel in channels)
            {
                if (channel.IsFrameworkOwned != framework) continue;

                string name = channel.Name;
                var button = new Button(() => ToggleChannel(name)) {text = name};
                button.AddToClassList("dd-channel");
                button.style.color = channel.Color;
                button.EnableInClassList(CHANNEL_ON_CLASS, !Filter.HiddenChannels.Contains(name));
                wrap.Add(button);
                _channelButtons[name] = button;
                count++;
            }

            if (count == 0) return;

            _channelRows.Add(heading);
            _channelRows.Add(wrap);
        }

        private void ToggleChannel(string name)
        {
            if (!Filter.HiddenChannels.Remove(name)) Filter.HiddenChannels.Add(name);

            if (_channelButtons.TryGetValue(name, out Button button))
                button.EnableInClassList(CHANNEL_ON_CLASS, !Filter.HiddenChannels.Contains(name));

            OnFilterChanged?.Invoke();
        }

        private void SetAllChannels(bool shown)
        {
            Filter.HiddenChannels.Clear();

            if (!shown)
            {
                foreach (string name in _channelButtons.Keys) Filter.HiddenChannels.Add(name);
            }

            foreach (KeyValuePair<string, Button> pair in _channelButtons)
                pair.Value.EnableInClassList(CHANNEL_ON_CLASS, shown);

            OnFilterChanged?.Invoke();
        }

        private void ToggleChannelPanel()
        {
            _channelsOpen = !_channelsOpen;
            _channelPanel.style.display = _channelsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _filters.EnableInClassList(FILTER_ON_CLASS, _channelsOpen);
        }

        // ======================== Detail ========================

        public void PaintDetail(ConsoleLog row)
        {
            _detailRow = row;

            if (row == null)
            {
                _detail.style.display = DisplayStyle.None;
                return;
            }

            LogRowTextVO text = _rowText.Split(row);
            string when = row.Hour.ToString("00") + ":" + row.Minute.ToString("00") + ":" + row.Second.ToString("00") + "." +
                          row.Millisecond.ToString("000");

            _detailHeading.text = when + "   " + row.LogType + "   " + text.Tag + "   " + text.Source;
            _detailHeading.style.color = text.TagColor;

            // The logger's "raise Source on the Flow Console's bar" hint names a bar a phone does not have.
            bool hasTrace = !string.IsNullOrEmpty(row.StackTrace) && !row.StackTrace.StartsWith("Source not captured", StringComparison.Ordinal);
            string trace = hasTrace ? "\n\n" + row.StackTrace.TrimEnd() : "";
            _detailText.text = text.PlainMessage + trace;
            _detail.style.display = DisplayStyle.Flex;

            // The list just lost a third of its height; the tapped row is brought back into view once
            // the new layout is in, a frame later.
            int index = _rows.IndexOf(row);

            if (index >= 0)
                _list.schedule.Execute(() => _list.ScrollToItem(index)).ExecuteLater(1);
        }

        public void ClearSelection() => PaintDetail(null);

        private void CloseDetail()
        {
            PaintDetail(null);
            OnRowSelected?.Invoke(null);
        }

        // ======================== Filters ========================

        private void ToggleFilter(ref bool flag, Button button)
        {
            flag = !flag;
            button.EnableInClassList(FILTER_ON_CLASS, flag);
            OnFilterChanged?.Invoke();
        }

        // ======================== A row ========================

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("dd-log-row");
            row.RegisterCallback<PointerDownEvent>(RowPressed);
            row.RegisterCallback<PointerUpEvent>(pointer => RowReleased(pointer, row));

            var icon = new SeverityIcon {name = "icon"};
            icon.AddToClassList("dd-log-icon");

            var lines = new VisualElement();
            lines.AddToClassList("dd-log-lines");

            var message = new Label {name = "message"};
            message.AddToClassList("dd-log-message");

            var second = new VisualElement();
            second.AddToClassList("dd-log-second");
            var tag = new Label {name = "tag"};
            tag.AddToClassList("dd-log-tag");
            var source = new Label {name = "source"};
            source.AddToClassList("dd-log-source");
            second.Add(tag);
            second.Add(source);

            lines.Add(message);
            lines.Add(second);
            row.Add(icon);
            row.Add(lines);

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _rows.Count) return;

            ConsoleLog row = _rows[index];
            LogRowTextVO text = _rowText.Split(row);

            element.userData = row;
            element.Q<SeverityIcon>("icon").Kind = row.LogType;
            element.Q<Label>("message").text = text.Message;

            Label tag = element.Q<Label>("tag");
            tag.text = text.Tag;
            tag.style.color = text.TagColor;
            element.Q<Label>("source").text = text.Source;
        }

        // A tap is a press and a release within a finger's width and half a second of each other.
        // A drag that scrolls the list moves further than that, and the ScrollView takes the pointer
        // once it scrolls, so the release never reaches the row anyway.
        private const float TAP_SLOP = 12f;
        private const float TAP_SECONDS = 0.5f;

        private void RowPressed(PointerDownEvent pointer)
        {
            _pressedAt = pointer.position;
            _pressedTime = Time.unscaledTime;
        }

        private void RowReleased(PointerUpEvent pointer, VisualElement row)
        {
            bool moved = ((Vector2) pointer.position - _pressedAt).sqrMagnitude > TAP_SLOP * TAP_SLOP;
            bool late = Time.unscaledTime - _pressedTime > TAP_SECONDS;

            if (moved || late) return;
            if (row.userData is not ConsoleLog log) return;

            OnRowSelected?.Invoke(log);
        }
    }
}