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

        private CD_FlowConsole _settings;
        private GUIStyle _richTextStyle;
        private GUIStyle _detailRichTextStyle;
        private GUIStyle _linkStyle;

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
                    LogGUI(_cachedVisibleLogs[i], _cachedLogHeights[i]);
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

            RebuildLogHeights();
        }

        private void RebuildLogHeights()
        {
            int count = _cachedVisibleLogs.Count;

            if (_cachedLogHeights == null || _cachedLogHeights.Length < count)
                _cachedLogHeights = new float[Mathf.Max(count, 64)];
            if (_cumulativeHeights == null || _cumulativeHeights.Length < count + 1)
                _cumulativeHeights = new float[Mathf.Max(count + 1, 65)];

            _cumulativeHeights[0] = 0;

            for (int i = 0; i < count; i++)
            {
                int lineCount = 1;
                string msg = _cachedVisibleLogs[i].Message;
                for (int c = 0; c < msg.Length; c++)
                {
                    if (msg[c] == '\n') lineCount++;
                }

                _cachedLogHeights[i] = lineCount * LogEntryLineHeight + LogEntryPadding;
                _cumulativeHeights[i + 1] = _cumulativeHeights[i] + _cachedLogHeights[i];
            }
        }

        private void LogGUI(ConsoleLog consoleLog, float entryHeight)
        {
            var bgColor = consoleLog.LogType == LogType.Log ? consoleLog.LogColor :
                consoleLog.LogType == LogType.Warning ? Color.yellow : Color.red;

            GUI.backgroundColor = bgColor;

            Rect rect = GUILayoutUtility.GetRect(0, entryHeight, GUILayout.ExpandWidth(true));
            GUI.Box(rect, GUIContent.none, "box");

            if (_selectedLog == consoleLog)
            {
                EditorGUI.DrawRect(rect, new Color(0.17f, 0.36f, 0.53f, 1f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(0.3f, 0.7f, 1f, 1f));
            }

            var date = consoleLog.Hour.ToString("00") + ":" + consoleLog.Minute.ToString("00") + ":" + consoleLog.Second.ToString("00") + ":" +
                       consoleLog.Millisecond.ToString("000");
            Rect textRect = new Rect(rect.x + 4, rect.y, rect.width - 8, rect.height);
            GUI.Label(textRect, date + " | " + consoleLog.Message, _richTextStyle);

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.button == 0)
                {
                    if (currentEvent.clickCount == 2)
                    {
                        bool hasFileInfo = !string.IsNullOrEmpty(consoleLog.SourceFilePath);
                        bool hasClassInfo = !string.IsNullOrEmpty(consoleLog.SourceClassName);

                        if (hasFileInfo || hasClassInfo)
                            TryOpenSourceFile(consoleLog);
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