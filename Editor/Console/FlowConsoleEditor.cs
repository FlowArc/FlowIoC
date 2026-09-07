#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    internal partial class FlowConsoleEditor : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/Flow Console", false, -1151)]
        private static void OpenConsole()
        {
            var window = GetWindow<FlowConsoleEditor>("Flow Console");
            // window.position = new Rect(window.position.position, new Vector2(800, 600));
            window.minSize = new Vector2(800, 600);
        }

//======================================================================================================================
//  ===========   Properties   ===========

        private ConsoleLog _selectedLog;

        private Dictionary<LogType, bool> _logFilter;
        private Vector2 _logsPanelScroll;
        private Vector2 _topPanelScroll;
        private Vector2 _detailPanelScroll;
        private float _detailPanelHeight = 150f;
        private bool _isResizingDetailPanel;

        private bool _needsRepaint;
        private List<ConsoleLog> _cachedFilteredLogs;
        private List<ConsoleLog> _cachedVisibleLogs;
        private bool _logsDirty = true;
        private int _cachedLogCount;
        private int _cachedWarningCount;
        private int _cachedErrorCount;
        private const float LogEntryLineHeight = 16f;
        private const float LogEntryPadding = 6f;
        private const int LogTrimChunk = 256;

        private float[] _cachedLogHeights;
        private float[] _cumulativeHeights;

        private List<ConsoleLog> _multiTypeFilterBuffer;

        private int _layoutFirstVisible;
        private int _layoutLastVisible;

        private List<ConsoleLog> _allLogs;
        private string _searchText = "";
        private bool _scrollToSelectedLog;
        private float _preSearchScrollY;
        private bool _logSelectedDuringSearch;

        private static readonly SystemLogType[] SystemLogTypeValues =
            (SystemLogType[]) Enum.GetValues(typeof(SystemLogType));

        private readonly FlowSourceNavigator _navigator = new();
        private readonly FlowConsoleState _state = new();
        private readonly FlowConsoleCollapse _collapse = new();

        private bool _collapseRows;
        private int[] _collapseCounts;

        private CD_FlowConsole _settings;
        private GUIStyle _richTextStyle;
        private GUIStyle _detailRichTextStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _secondLineStyle;

        // Unity's own console icons, fetched once. IconContent is a lookup and a row draws many
        // times a second.
        private Texture _infoIcon;
        private Texture _warningIcon;
        private Texture _errorIcon;
        private Texture _infoIconSmall;
        private Texture _warningIconSmall;
        private Texture _errorIconSmall;

        /// <summary>
        /// How many lines of a message a row shows. Unity's console shows two - the message and
        /// the frame it came from - and that is what this starts on.
        /// </summary>
        private int _rowLineCount = 2;

        private ConsoleLog _cachedTraceLog;
        private string[] _cachedTraceLines;
        private string[] _cachedTraceDisplayTexts;
        private string[] _cachedTraceFilePaths;
        private string[] _cachedTraceClassNames;
        private int[] _cachedTraceLineNumbers;


        private void OnEnable()
        {
            _selectedLog = null;
            InvalidateTraceCache();

            _rowLineCount = _state.RowLineCount;
            _collapseRows = _state.Collapse;

            _logFilter = new Dictionary<LogType, bool>
            {
                {LogType.Log, true},
                {LogType.Warning, true},
                {LogType.Error, true}
            };

            InitializeLogs();

            _cachedVisibleLogs = new List<ConsoleLog>();
            _cachedFilteredLogs = _allLogs;
            _logsDirty = true;

            FlowLogger.OnLogAdded += OnLogAdded;
            CD_FlowConsole.OnSettingsValidated += OnSettingsValidated;

            _settings = FlowLogger.Settings;

            _richTextStyle = new GUIStyle();
            _richTextStyle.richText = true;
            _richTextStyle.wordWrap = false;
            _richTextStyle.clipping = TextClipping.Clip;
            _richTextStyle.normal.textColor = Color.white;

            _detailRichTextStyle = new GUIStyle();
            _detailRichTextStyle.richText = true;
            _detailRichTextStyle.wordWrap = true;
            _detailRichTextStyle.normal.textColor = Color.white;

            _linkStyle = new GUIStyle();
            _linkStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
            _linkStyle.hover.textColor = new Color(0.5f, 0.85f, 1f);
            _linkStyle.active.textColor = new Color(0.2f, 0.6f, 0.9f);
            _linkStyle.wordWrap = false;

            // Unity ships each console icon at two sizes. Scaling the large one down to 14px is
            // what made the shapes come out ragged, so each is drawn at the size it was authored
            // for: the small one on a one-line row, the large one on a two-line row.
            _secondLineStyle = new GUIStyle();
            _secondLineStyle.richText = false;
            _secondLineStyle.wordWrap = false;
            _secondLineStyle.clipping = TextClipping.Clip;
            _secondLineStyle.fontSize = 10;
            _secondLineStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);

            _infoIcon = EditorGUIUtility.IconContent("console.infoicon").image;
            _warningIcon = EditorGUIUtility.IconContent("console.warnicon").image;
            _errorIcon = EditorGUIUtility.IconContent("console.erroricon").image;

            _infoIconSmall = EditorGUIUtility.IconContent("console.infoicon.sml").image;
            _warningIconSmall = EditorGUIUtility.IconContent("console.warnicon.sml").image;
            _errorIconSmall = EditorGUIUtility.IconContent("console.erroricon.sml").image;
        }

        /// <summary>
        /// Why a double-click went nowhere. Nearly always the capture setting rather than a
        /// missing file, so the answer names the switch that fixes it.
        /// </summary>
        private string NoSourceHint()
        {
            return _settings.StackTraceCapture == FlowStackTraceCapture.Always
                ? "No source was found for this log."
                : "This log captured no source.\nRaise Source in the toolbar to Always.";
        }

        /// <summary>
        /// What each capture setting costs, said where it is chosen. Always is what makes an
        /// ordinary log clickable, and it is also the most expensive thing the console does.
        /// </summary>
        private static string CaptureLabel(FlowStackTraceCapture capture)
        {
            switch (capture)
            {
                case FlowStackTraceCapture.Never: return "Never  ·  cheapest, nothing is clickable";
                case FlowStackTraceCapture.WarningsAndErrors: return "Warnings and errors  ·  the default";
                default: return "Always  ·  every log clickable, costs the most";
            }
        }

        /// <summary>
        /// The first line of a message. A row is one height for every log, so a message carrying
        /// newlines is read in the detail panel rather than pushing the rows below it off screen.
        /// </summary>
        private static string LineOf(string message, int index)
        {
            if (string.IsNullOrEmpty(message)) return index == 0 ? string.Empty : null;

            int start = 0;

            for (int i = 0; i < index; i++)
            {
                int next = message.IndexOf('\n', start);
                if (next < 0) return null;
                start = next + 1;
            }

            int end = message.IndexOf('\n', start);
            return end < 0 ? message.Substring(start) : message.Substring(start, end - start);
        }

        /// <summary>
        /// What a two-line row says underneath the message: where the log came from, or the
        /// channel when nothing was captured. Never left blank, because an empty second line
        /// reads as a rendering fault rather than as missing information.
        /// </summary>
        private static string SecondLineFor(ConsoleLog log)
        {
            if (!string.IsNullOrEmpty(log.SourceFilePath))
                return Path.GetFileName(log.SourceFilePath) + ":" + log.SourceLineNumber;

            if (!string.IsNullOrEmpty(log.BlameTypeName))
                return log.BlameTypeName;

            if (!string.IsNullOrEmpty(log.SourceClassName))
                return log.SourceClassName;

            // Said on the row rather than discovered by double-clicking it and getting nothing.
            // Ordinary logs capture no source by default, because working one out builds the whole
            // managed stack as a string and the framework writes a log per signal and command.
            if (FlowLogger.Settings.StackTraceCapture != FlowStackTraceCapture.Always)
                return log.SystemLogType + "  ·  source not captured";

            return log.SystemLogType.ToString();
        }

        private Texture IconFor(LogType logType, bool small)
        {
            switch (logType)
            {
                case LogType.Warning: return small ? _warningIconSmall : _warningIcon;
                case LogType.Log: return small ? _infoIconSmall : _infoIcon;
                default: return small ? _errorIconSmall : _errorIcon;
            }
        }

        private void OnDisable()
        {
            FlowLogger.OnLogAdded -= OnLogAdded;
            CD_FlowConsole.OnSettingsValidated -= OnSettingsValidated;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawToolbar();

            TopPanelGUI();
            LogsPanelGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                FlowLogger.ClearLogs();
                InitializeLogs();
                _selectedLog = null;
                InvalidateTraceCache();
                _searchText = "";
                _logsDirty = true;
                Repaint();
            }

            bool collapse = GUILayout.Toggle(_collapseRows, "Collapse", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (collapse != _collapseRows)
            {
                _collapseRows = collapse;
                _state.Collapse = collapse;
                _logsDirty = true;
                _needsRepaint = true;
            }

            GUILayout.FlexibleSpace();

            string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(200));
            if (newSearch != _searchText)
            {
                bool startingSearch = string.IsNullOrEmpty(_searchText) && !string.IsNullOrEmpty(newSearch);
                bool clearingSearch = !string.IsNullOrEmpty(_searchText) && string.IsNullOrEmpty(newSearch);

                if (startingSearch)
                {
                    _preSearchScrollY = _logsPanelScroll.y;
                    _logSelectedDuringSearch = false;
                }

                if (clearingSearch)
                {
                    if (_logSelectedDuringSearch && _selectedLog != null)
                        _scrollToSelectedLog = true;
                    else
                        _logsPanelScroll.y = _preSearchScrollY;
                }

                _searchText = newSearch;
                _logsDirty = true;
                _needsRepaint = true;
            }

            EditorGUI.BeginDisabledGroup(_selectedLog == null);
            if (GUILayout.Button("Locate", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _scrollToSelectedLog = true;
                _needsRepaint = true;
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(_rowLineCount + " line" + (_rowLineCount == 1 ? "" : "s"),
                    EditorStyles.toolbarDropDown, GUILayout.Width(60)))
            {
                var menu = new GenericMenu();

                for (int lines = 1; lines <= 3; lines++)
                {
                    int chosen = lines;
                    menu.AddItem(new GUIContent(lines + " line" + (lines == 1 ? "" : "s")),
                        _rowLineCount == lines,
                        () =>
                        {
                            _rowLineCount = chosen;
                            _state.RowLineCount = chosen;
                            _logsDirty = true;
                            _needsRepaint = true;
                        });
                }

                menu.ShowAsContext();
            }

            // The setting that decides whether double-clicking a row can go anywhere. It lives in
            // the settings asset, but it is raised and lowered while following one flow, so it
            // belongs where the flow is being read rather than three windows away.
            FlowStackTraceCapture capture = _settings.StackTraceCapture;
            if (GUILayout.Button("Source: " + capture, EditorStyles.toolbarDropDown, GUILayout.Width(150)))
            {
                var menu = new GenericMenu();

                foreach (FlowStackTraceCapture value in Enum.GetValues(typeof(FlowStackTraceCapture)))
                {
                    FlowStackTraceCapture chosen = value;
                    menu.AddItem(new GUIContent(CaptureLabel(value)), capture == value, () =>
                    {
                        _settings.StackTraceCapture = chosen;
                        EditorUtility.SetDirty(_settings);
                        _needsRepaint = true;
                    });
                }

                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Update()
        {
            if (_needsRepaint)
            {
                _needsRepaint = false;
                Repaint();
            }
        }

        private void OnSettingsValidated()
        {
            _logsDirty = true;
            _needsRepaint = true;
        }

        private void InitializeLogs()
        {
            _allLogs = new List<ConsoleLog>(FlowLogger.Logs);
        }

        private void TopPanelGUI()
        {
            EditorGUILayout.BeginVertical();

            _topPanelScroll = EditorGUILayout.BeginScrollView(
                _topPanelScroll,
                true,
                false,
                GUI.skin.horizontalScrollbar,
                GUIStyle.none,
                GUI.skin.box,
                GUILayout.Height(45),
                GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            for (var ii = 0; ii < SystemLogTypeValues.Length; ii++)
            {
                var consoleLogType = SystemLogTypeValues[ii];
                if (consoleLogType == SystemLogType.All)
                {
                    bool allVisible = IsAllSystemTypesVisible();
                    GUI.backgroundColor = allVisible ? Color.green : Color.white;

                    if (GUILayout.Button("All", GUILayout.MinWidth(80)))
                    {
                        SetAllSystemTypesVisible(!allVisible);
                        OnLogTypeSelectionChanged();
                    }

                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (!_settings.TryGetLogType((int) consoleLogType, out var typeInfo)) continue;

                    GUI.backgroundColor = typeInfo.IsVisible ? Color.green : Color.white;

                    if (GUILayout.Button(consoleLogType.ToString(), GUILayout.MinWidth(80)))
                    {
                        typeInfo.IsVisible = !typeInfo.IsVisible;
                        EditorUtility.SetDirty(_settings);
                        OnLogTypeSelectionChanged();
                    }

                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            TopPanelConsoleFilterGUI();

            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void LogsPanelGUI()
        {
            RebuildCachedLogs();

            if (_scrollToSelectedLog && _selectedLog != null && Event.current.type == EventType.Layout)
                ScrollToSelectedLog();

            int totalCount = _cachedVisibleLogs.Count;

            _logsPanelScroll = EditorGUILayout.BeginScrollView(_logsPanelScroll);

            if (totalCount > 0 && _cumulativeHeights != null)
            {
                if (Event.current.type == EventType.Layout)
                {
                    float viewportHeight = position.height;
                    float scrollY = _logsPanelScroll.y;

                    int lo = 0, hi = totalCount;
                    while (lo < hi)
                    {
                        int mid = (lo + hi) / 2;
                        if (_cumulativeHeights[mid + 1] <= scrollY)
                            lo = mid + 1;
                        else
                            hi = mid;
                    }

                    _layoutFirstVisible = Mathf.Clamp(lo, 0, totalCount - 1);

                    float visibleEnd = scrollY + viewportHeight;
                    _layoutLastVisible = _layoutFirstVisible;
                    for (int i = _layoutFirstVisible; i < totalCount; i++)
                    {
                        if (_cumulativeHeights[i] >= visibleEnd)
                            break;
                        _layoutLastVisible = i;
                    }

                    _layoutLastVisible = Mathf.Min(_layoutLastVisible + 1, totalCount - 1);
                }

                int firstVisible = Mathf.Clamp(_layoutFirstVisible, 0, totalCount - 1);
                int lastVisible = Mathf.Clamp(_layoutLastVisible, firstVisible, totalCount - 1);

                float spaceBefore = _cumulativeHeights[firstVisible];
                if (spaceBefore > 0)
                    GUILayout.Space(spaceBefore);

                for (int i = firstVisible; i <= lastVisible; i++)
                {
                    LogGUI(_cachedVisibleLogs[i], _cachedLogHeights[i], i);
                }

                float spaceAfter = _cumulativeHeights[totalCount] - _cumulativeHeights[lastVisible + 1];
                if (spaceAfter > 0)
                    GUILayout.Space(spaceAfter);
            }

            EditorGUILayout.EndScrollView();


            DetailPanelGUI();
        }

        private void RebuildCachedLogs()
        {
            if (!_logsDirty) return;
            if (Event.current != null && Event.current.type != EventType.Layout) return;
            _logsDirty = false;

            bool allTypesVisible = true;
            if (_settings != null && _settings.LogTypes != null)
            {
                for (int i = 0; i < _settings.LogTypes.Count; i++)
                {
                    var lt = _settings.LogTypes[i];
                    if (lt.Value == (int) SystemLogType.All) continue;
                    if (!lt.IsVisible)
                    {
                        allTypesVisible = false;
                        break;
                    }
                }
            }

            if (allTypesVisible)
            {
                _cachedFilteredLogs = _allLogs;
            }
            else
            {
                var allLogs = _allLogs;
                if (_multiTypeFilterBuffer == null)
                    _multiTypeFilterBuffer = new List<ConsoleLog>();
                else
                    _multiTypeFilterBuffer.Clear();

                for (int i = 0; i < allLogs.Count; i++)
                {
                    var log = allLogs[i];

                    bool isSystemLog = log.SystemLogType != SystemLogType.All;

                    if (isSystemLog)
                    {
                        if (_settings.TryGetLogType((int) log.SystemLogType, out var sysType) && sysType.IsVisible)
                        {
                            _multiTypeFilterBuffer.Add(log);
                            continue;
                        }
                    }

                    if (_settings.TryGetLogType(log.LogTypeValue, out var projType) && !projType.IsMandatory && projType.IsVisible)
                    {
                        _multiTypeFilterBuffer.Add(log);
                    }
                }

                _cachedFilteredLogs = _multiTypeFilterBuffer;
            }

            _cachedLogCount = 0;
            _cachedWarningCount = 0;
            _cachedErrorCount = 0;
            for (int i = 0; i < _cachedFilteredLogs.Count; i++)
            {
                switch (_cachedFilteredLogs[i].LogType)
                {
                    case LogType.Log: _cachedLogCount++; break;
                    case LogType.Warning: _cachedWarningCount++; break;
                    case LogType.Error: _cachedErrorCount++; break;
                }
            }

            RebuildVisibleLogs();

            if (_selectedLog != null && !_cachedVisibleLogs.Contains(_selectedLog))
            {
                _selectedLog = null;
                InvalidateTraceCache();
            }

            if (_cumulativeHeights != null && _cachedVisibleLogs.Count > 0)
            {
                float totalContentHeight = _cumulativeHeights[_cachedVisibleLogs.Count];
                if (_logsPanelScroll.y > totalContentHeight)
                    _logsPanelScroll.y = 0;
            }
            else
            {
                _logsPanelScroll.y = 0;
            }
        }

        private void RebuildVisibleLogs()
        {
            if (_cachedVisibleLogs == null)
                _cachedVisibleLogs = new List<ConsoleLog>();
            else
                _cachedVisibleLogs.Clear();

            if (_cachedFilteredLogs == null) return;

            bool hasSearch = !string.IsNullOrEmpty(_searchText);

            for (int i = 0; i < _cachedFilteredLogs.Count; i++)
            {
                var log = _cachedFilteredLogs[i];
                if (!_logFilter[log.LogType]) continue;
                if (hasSearch && log.Message.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _cachedVisibleLogs.Add(log);
            }

            FoldVisibleLogs();
            RebuildLogHeights();
        }

        /// <summary>
        /// Folds equal rows into counted ones when Collapse is on. The visible list keeps holding
        /// one log per row - the first occurrence - and the counts travel beside it, so nothing
        /// downstream has to know whether folding happened.
        /// </summary>
        private void FoldVisibleLogs()
        {
            if (!_collapseRows)
            {
                _collapseCounts = null;
                return;
            }

            List<CollapsedRow> rows = _collapse.Fold(_cachedVisibleLogs);

            _cachedVisibleLogs.Clear();

            if (_collapseCounts == null || _collapseCounts.Length < rows.Count)
                _collapseCounts = new int[Mathf.Max(rows.Count, 64)];

            for (int i = 0; i < rows.Count; i++)
            {
                _cachedVisibleLogs.Add(rows[i].Log);
                _collapseCounts[i] = rows[i].Count;
            }
        }

        private void RebuildLogHeights()
        {
            int count = _cachedVisibleLogs.Count;

            if (_cachedLogHeights == null || _cachedLogHeights.Length < count)
                _cachedLogHeights = new float[Mathf.Max(count, 64)];
            if (_cumulativeHeights == null || _cumulativeHeights.Length < count + 1)
                _cumulativeHeights = new float[Mathf.Max(count + 1, 65)];

            _cumulativeHeights[0] = 0;

            // Every row is the same height, the way Unity's console draws it. A message longer
            // than the row is clipped and read in full in the detail panel; letting one log with
            // forty newlines take the whole window is what the clamp is for.
            float rowHeight = _rowLineCount * LogEntryLineHeight + LogEntryPadding;

            for (int i = 0; i < count; i++)
            {
                _cachedLogHeights[i] = rowHeight;
                _cumulativeHeights[i + 1] = _cumulativeHeights[i] + rowHeight;
            }
        }

        private void LogGUI(ConsoleLog consoleLog, float entryHeight, int rowIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(0, entryHeight, GUILayout.ExpandWidth(true));

            // Unity's console reads severity from an icon and uses the row background only to
            // separate one row from the next. Tinting a whole row yellow or red made a page of
            // warnings unreadable, so the severity moved to the icon and the row keeps the
            // alternating band.
            if (rowIndex % 2 == 1)
                EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.08f));

            if (_selectedLog == consoleLog)
                EditorGUI.DrawRect(rect, new Color(0.17f, 0.36f, 0.53f, 1f));

            // The one piece of information Unity's console has no equivalent for: which channel
            // wrote this. It keeps the colour the settings give the channel.
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), consoleLog.LogColor);

            bool small = _rowLineCount < 2;
            Texture icon = IconFor(consoleLog.LogType, small);

            // Drawn at the size the texture was authored for - 16 for the .sml variant, 32 for the
            // large one - because scaling either of them is what made the shapes come out ragged.
            if (icon != null)
            {
                float size = small ? 16f : 32f;
                float y = rect.y + (rect.height - size) * 0.5f;
                GUI.DrawTexture(new Rect(rect.x + 6f, y, size, size), icon, ScaleMode.StretchToFill);
            }

            float textLeft = rect.x + (small ? 26f : 42f);
            var date = consoleLog.Hour.ToString("00") + ":" + consoleLog.Minute.ToString("00") + ":" + consoleLog.Second.ToString("00") + ":" +
                       consoleLog.Millisecond.ToString("000");

            float textWidth = rect.width - (textLeft - rect.x) - 4f;

            // The count of a folded row, right-aligned so the messages stay lined up under each
            // other. Drawn before the text so the text knows how much room it has left.
            int foldedCount = _collapseRows && _collapseCounts != null && rowIndex < _collapseCounts.Length
                ? _collapseCounts[rowIndex]
                : 1;

            if (foldedCount > 1)
            {
                var badge = new GUIContent(foldedCount.ToString());
                Vector2 badgeSize = EditorStyles.miniButton.CalcSize(badge);
                float badgeWidth = Mathf.Max(badgeSize.x, 22f);

                var badgeRect = new Rect(rect.xMax - badgeWidth - 6f,
                    rect.y + (rect.height - 16f) * 0.5f, badgeWidth, 16f);

                GUI.Label(badgeRect, badge, EditorStyles.miniButton);
                textWidth -= badgeWidth + 10f;
            }

            // Unity's own row: the message, and underneath it where the message came from. That
            // last line is what tells a reader which of forty identical warnings is theirs, so it
            // follows the message rather than sitting at the bottom of the row.
            int messageSlots = small ? _rowLineCount : _rowLineCount - 1;

            int drawnLines = 0;
            while (drawnLines < messageSlots && LineOf(consoleLog.Message, drawnLines) != null)
                drawnLines++;

            if (drawnLines == 0) drawnLines = 1;

            // Centred on what is actually drawn, not on what the row could hold. Centring on the
            // maximum left a gap in the middle of a three-line row whenever the message was one
            // line long, and the source line ended up stranded at the bottom.
            float blockHeight = (drawnLines + (small ? 0 : 1)) * LogEntryLineHeight;
            float textTop = rect.y + (rect.height - blockHeight) * 0.5f;

            for (int line = 0; line < drawnLines; line++)
            {
                string text = LineOf(consoleLog.Message, line) ?? string.Empty;

                Rect lineRect = new Rect(textLeft, textTop + line * LogEntryLineHeight, textWidth, LogEntryLineHeight);
                GUI.Label(lineRect, line == 0 ? date + " | " + text : text, _richTextStyle);
            }

            if (!small)
            {
                Rect sourceRect = new Rect(textLeft, textTop + drawnLines * LogEntryLineHeight,
                    textWidth, LogEntryLineHeight);
                GUI.Label(sourceRect, SecondLineFor(consoleLog), _secondLineStyle);
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.button == 0)
                {
                    if (currentEvent.clickCount == 2)
                    {
                        _selectedLog = consoleLog;

                        // Silently doing nothing reads as a broken window. The row either opens
                        // its file or says why it cannot.
                        if (!_navigator.Open(consoleLog))
                            ShowNotification(new GUIContent(NoSourceHint()), 3d);
                    }
                    else
                    {
                        _selectedLog = consoleLog;
                        if (!string.IsNullOrEmpty(_searchText))
                            _logSelectedDuringSearch = true;
                        _detailPanelScroll = Vector2.zero;
                    }

                    Repaint();
                    currentEvent.Use();
                }
                else if (currentEvent.button == 1)
                {
                    GenericMenu menu = new GenericMenu();

                    bool hasFileInfo = !string.IsNullOrEmpty(consoleLog.SourceFilePath);
                    bool hasClassInfo = !string.IsNullOrEmpty(consoleLog.SourceClassName);

                    if (hasFileInfo || hasClassInfo)
                    {
                        menu.AddItem(new GUIContent("Open File"), false, () => { TryOpenSourceFile(consoleLog); });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Open File"));
                    }

                    menu.AddItem(new GUIContent("Copy"), false, () => { EditorGUIUtility.systemCopyBuffer = consoleLog.Message; });

                    menu.ShowAsContext();
                    currentEvent.Use();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void ScrollToSelectedLog()
        {
            _scrollToSelectedLog = false;

            for (int i = 0; i < _cachedVisibleLogs.Count; i++)
            {
                if (_cachedVisibleLogs[i] != _selectedLog) continue;

                if (_cumulativeHeights != null && _cumulativeHeights.Length > i + 1)
                {
                    float logTop = _cumulativeHeights[i];
                    float logHeight = _cachedLogHeights[i];
                    float viewportHeight = position.height;
                    _logsPanelScroll.y = logTop - (viewportHeight - logHeight) / 2f;
                    _logsPanelScroll.y = Mathf.Max(0, _logsPanelScroll.y);
                }

                return;
            }
        }

        private void OnLogTypeSelectionChanged()
        {
            _logsDirty = true;
            _needsRepaint = true;
            Repaint();
        }

        private void OnLogAdded(ConsoleLog log)
        {
            _allLogs.Add(log);
            TrimToMaxLogCount();
            _logsDirty = true;
            _needsRepaint = true;
        }

        /// <summary>
        /// The window keeps its own copy of the logs, so it has to honour the same limit the
        /// settings put on FlowLogger - otherwise a window left open through a long session grows
        /// without end while the list it was built from stays capped.
        /// </summary>
        private void TrimToMaxLogCount()
        {
            int maxLogCount = FlowLogger.Settings.MaxLogCount;
            if (maxLogCount <= 0 || _allLogs.Count <= maxLogCount + LogTrimChunk)
                return;

            _allLogs.RemoveRange(0, _allLogs.Count - maxLogCount);
        }
    }
}
#endif