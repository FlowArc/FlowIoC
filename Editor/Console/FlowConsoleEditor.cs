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

        private readonly FlowConsoleStickyTail _stickyTail = new FlowConsoleStickyTail();
        private readonly FlowConsoleKeyboard _keyboard = new FlowConsoleKeyboard();
        private float _logsViewportHeight;

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

        /// <summary>
        /// Paths that came out of a stack trace are whatever text was there, so they are taken
        /// apart by hand rather than by System.IO.Path, which throws on characters Windows does
        /// not allow - and a throw inside OnGUI takes the window's drawing down with it.
        /// </summary>
        private static readonly FlowStackFrameFilter PathText = new();

        private bool _collapseRows;
        private int[] _collapseCounts;

        private CD_FlowConsole _settings;
        private GUIStyle _richTextStyle;
        private GUIStyle _detailRichTextStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _secondLineStyle;
        private GUIStyle _toolbarLabelStyle;

        private Rect _toolbarRect;
        private bool _mouseWasOverToolbar;

        // Unity's own console icons, fetched once. IconContent is a lookup and a row draws many
        // times a second.
        private Texture _infoIcon;
        private Texture _warningIcon;
        private Texture _errorIcon;
        private Texture _dropdownIcon;
        private static readonly GUIContent ClearLabel = new("Clear");

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

            // Without this the window is never told the pointer moved, and a toolbar button only
            // turns hovered on the next repaint something else happens to cause.
            wantsMouseMove = true;

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
            FlowLogger.OnLogsCleared += OnLogsCleared;
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

            _dropdownIcon = EditorGUIUtility.IconContent("icon dropdown").image;

            // The toolbar label style is not built here. OnEnable can run before EditorStyles is
            // ready, and what it hands back then is not the style that was asked for - a probe
            // showed the label still carrying the toolbar button's own graphics, which is what
            // painted over the Clear half. It is built on the first repaint instead.
            _toolbarLabelStyle = null;

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
                return PathText.FileNameOf(log.SourceFilePath) + ":" + log.SourceLineNumber;

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
            FlowLogger.OnLogsCleared -= OnLogsCleared;
            CD_FlowConsole.OnSettingsValidated -= OnSettingsValidated;
        }

        private void OnGUI()
        {
            RepaintForToolbarHover();

            EditorGUILayout.BeginVertical();

            DrawToolbar();

            TopPanelGUI();
            LogsPanelGUI();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// A toolbar button only looks hovered while the window is repainting, and a console that
        /// is not receiving logs repaints for nothing - which is why hovering used to take about a
        /// second to show. The window asks for mouse-move events and repaints on them, but only
        /// while the pointer is over the toolbar, or has just left it. Moving across the log list
        /// still costs nothing.
        /// </summary>
        private void RepaintForToolbarHover()
        {
            if (Event.current.type != EventType.MouseMove) return;

            // Nothing has been drawn yet, so there is no toolbar to test against. Repaint once to
            // get one.
            if (_toolbarRect.height <= 0f)
            {
                Repaint();
                return;
            }

            bool overToolbar = _toolbarRect.Contains(Event.current.mousePosition);
            if (!overToolbar && !_mouseWasOverToolbar) return;

            _mouseWasOverToolbar = overToolbar;
            Repaint();
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Kept only from a repaint. BeginHorizontal answers with an empty rect during a
            // layout pass, and storing that wiped the real one just before the mouse-move event
            // that needed it - which is why hovering was quick coming from Unity's own controls
            // and slow everywhere else.
            if (Event.current.type == EventType.Repaint)
                _toolbarRect = toolbarRect;

            ClearButtonGUI();

            bool errorPause = GUILayout.Toggle(_state.ErrorPause, "Error Pause", EditorStyles.toolbarButton,
                GUILayout.Width(80));
            if (errorPause != _state.ErrorPause)
                _state.ErrorPause = errorPause;

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

        /// <summary>
        /// Clear and the arrow beside it, drawn as the one split button Unity's console has.
        ///
        /// EditorStyles.toolbarDropDown cannot make this shape: it draws its own border and its
        /// own arrow, which put the divider hard against the glyph and ran it the full height of
        /// the bar. Here the two halves share one background, the divider is inset top and bottom
        /// because it belongs to the button rather than to the toolbar, hovering either half
        /// lights the whole thing, and hovering the arrow lights the arrow again on top.
        /// </summary>
        /// <summary>
        /// Built on demand, inside OnGUI, where the editor's styles are certain to be ready.
        /// GUIStyle.none carries no graphics of any kind, so nothing here can paint over what the
        /// button drew underneath it.
        /// </summary>
        private void EnsureToolbarLabelStyle()
        {
            if (_toolbarLabelStyle != null) return;

            _toolbarLabelStyle = new GUIStyle(GUIStyle.none)
            {
                name = "FlowConsoleToolbarLabel",
                alignment = TextAnchor.MiddleCenter,
                fontSize = EditorStyles.toolbarButton.fontSize,
                font = EditorStyles.toolbarButton.font
            };

            _toolbarLabelStyle.normal.textColor = EditorStyles.label.normal.textColor;
        }

        private void ClearButtonGUI()
        {
            EnsureToolbarLabelStyle();

            const float clearWidth = 46f;
            const float arrowWidth = 22f;

            // The height is the toolbar row's, not a number of our own. Asking for
            // singleLineHeight made this button two pixels shorter than the ones beside it, which
            // left an unlit strip along the bottom and sat the label higher than Collapse's.
            Rect whole = GUILayoutUtility.GetRect(clearWidth + arrowWidth, EditorGUIUtility.singleLineHeight,
                EditorStyles.toolbarButton,
                GUILayout.Width(clearWidth + arrowWidth), GUILayout.ExpandHeight(true));

            var clearRect = new Rect(whole.x, whole.y, clearWidth, whole.height);
            var arrowRect = new Rect(whole.x + clearWidth, whole.y, arrowWidth, whole.height);

            Vector2 mouse = Event.current.mousePosition;
            bool overWhole = whole.Contains(mouse);
            bool overArrow = arrowRect.Contains(mouse);

            if (Event.current.type == EventType.Repaint)
            {
                // Every pixel here is drawn by hand. EditorStyles.toolbarButton was doing two
                // things that could not be switched off: its background carries borders, and the
                // one at the arrow's left edge landed exactly on the divider and read as a second
                // line beside it; and its hover state could not be made to light one half more
                // than the other without drawing a second button, which brought the border back.
                bool pro = EditorGUIUtility.isProSkin;

                // The row runs to the bottom of the toolbar, and the toolbar's own dark edge is
                // the last pixel of it. A wash painted over the full height covered that line, so
                // the lit area stops one pixel short of it.
                var wholeFill = new Rect(whole.x, whole.y, whole.width, whole.height - 1f);
                var arrowFill = new Rect(arrowRect.x, arrowRect.y, arrowRect.width, arrowRect.height - 1f);

                if (overWhole)
                {
                    EditorGUI.DrawRect(wholeFill, pro
                        ? new Color(1f, 1f, 1f, 0.08f)
                        : new Color(0f, 0f, 0f, 0.06f));
                }

                // Hovering the arrow lights the arrow further while the Clear half stays lit
                // underneath, which is the shape Unity's split button has.
                if (overArrow)
                {
                    EditorGUI.DrawRect(arrowFill, pro
                        ? new Color(1f, 1f, 1f, 0.10f)
                        : new Color(0f, 0f, 0f, 0.08f));
                }

                // The label alone, with no background of its own.
                GUI.Label(clearRect, ClearLabel, _toolbarLabelStyle);

                // Inset so the divider reads as part of the button rather than as a cut through
                // the toolbar, which is what running it the full height looked like.
                var divider = new Rect(arrowRect.x, whole.y + 4f, 1f, whole.height - 8f);
                EditorGUI.DrawRect(divider, new Color(0f, 0f, 0f, 0.35f));

                if (_dropdownIcon != null)
                {
                    const float glyphSize = 12f;
                    var glyph = new Rect(arrowRect.x + (arrowWidth - glyphSize) * 0.5f,
                        whole.y + (whole.height - glyphSize) * 0.5f, glyphSize, glyphSize);
                    GUI.DrawTexture(glyph, _dropdownIcon, ScaleMode.ScaleToFit);
                }
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;

            if (overArrow)
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Clear on Play"), _state.ClearOnPlay,
                    () => _state.ClearOnPlay = !_state.ClearOnPlay);
                menu.AddItem(new GUIContent("Clear on Recompile"), _state.ClearOnRecompile,
                    () => _state.ClearOnRecompile = !_state.ClearOnRecompile);
                menu.AddItem(new GUIContent("Clear on Build"), _state.ClearOnBuild,
                    () => _state.ClearOnBuild = !_state.ClearOnBuild);

                // Hung under the button's own left edge, the way Unity's does, rather than
                // wherever the pointer happened to be.
                menu.DropDown(new Rect(whole.x, whole.yMax, 0f, 0f));
                Event.current.Use();
                return;
            }

            if (!clearRect.Contains(mouse)) return;

            FlowLogger.ClearLogs();
            _searchText = "";
            Event.current.Use();
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

        /// <summary>
        /// Somebody emptied the list - Clear on Play, Clear on Recompile, or a call from game
        /// code. The window holds its own copy, so it has to let go of it too.
        /// </summary>
        private void OnLogsCleared()
        {
            InitializeLogs();
            _selectedLog = null;
            InvalidateTraceCache();
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

            HandleListKeys();

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

            // How tall the list actually is decides where its bottom sits, and only a repaint
            // knows - during Layout the rect is still a placeholder. Resizing the window or
            // dragging the detail panel moves that bottom, so a view resting on it follows.
            if (Event.current.type == EventType.Repaint)
            {
                float height = GUILayoutUtility.GetLastRect().height;

                if (height > 0f && !Mathf.Approximately(height, _logsViewportHeight))
                {
                    bool wasAtBottom = _stickyTail.IsAtBottom(
                        _logsPanelScroll.y, _logsViewportHeight, ContentHeight());

                    _logsViewportHeight = height;

                    float follow = _stickyTail.Follow(
                        wasAtBottom, _logsPanelScroll.y, height, ContentHeight());

                    if (!Mathf.Approximately(follow, _logsPanelScroll.y))
                    {
                        _logsPanelScroll.y = follow;
                        Repaint();
                    }
                }
            }

            DetailPanelGUI();
        }

        private void RebuildCachedLogs()
        {
            if (!_logsDirty) return;
            if (Event.current != null && Event.current.type != EventType.Layout) return;
            _logsDirty = false;

            // Asked before the list is rebuilt, because where the view was resting is only
            // answerable against the content it was resting in.
            bool wasAtBottom = _stickyTail.IsAtBottom(_logsPanelScroll.y, _logsViewportHeight, ContentHeight());

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

            _logsPanelScroll.y = _stickyTail.Follow(
                wasAtBottom, _logsPanelScroll.y, _logsViewportHeight, ContentHeight());
        }

        /// <summary>
        /// Walking the list with the keyboard. Handled before the scroll view is opened, so a key
        /// this window answers never also reaches the scroll view underneath it. While a text
        /// field is being edited the keyboard belongs to that field - the search box takes its own
        /// arrow keys.
        /// </summary>
        private void HandleListKeys()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown) return;
            if (EditorGUIUtility.editingTextField) return;

            int count = _cachedVisibleLogs?.Count ?? 0;
            if (count == 0) return;

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedLog != null && !_navigator.Open(_selectedLog))
                    ShowNotification(new GUIContent(NoSourceHint()), 3d);

                currentEvent.Use();
                return;
            }

            if ((currentEvent.control || currentEvent.command) && currentEvent.keyCode == KeyCode.C)
            {
                if (_selectedLog != null)
                    EditorGUIUtility.systemCopyBuffer = CopyTextOf(_selectedLog);

                currentEvent.Use();
                return;
            }

            int index = _selectedLog == null ? -1 : _cachedVisibleLogs.IndexOf(_selectedLog);
            float rowHeight = _rowLineCount * LogEntryLineHeight + LogEntryPadding;
            int rowsPerPage = Mathf.FloorToInt(_logsViewportHeight / rowHeight);

            if (!_keyboard.TryMove(currentEvent.keyCode, index, count, rowsPerPage, out int moved)) return;

            _selectedLog = _cachedVisibleLogs[moved];
            _detailPanelScroll = Vector2.zero;
            InvalidateTraceCache();

            if (_cumulativeHeights != null && _cumulativeHeights.Length > moved)
            {
                _logsPanelScroll.y = _keyboard.Reveal(
                    _logsPanelScroll.y, _logsViewportHeight, _cumulativeHeights[moved], rowHeight);
            }

            Repaint();
            currentEvent.Use();
        }

        /// <summary>What Ctrl+C puts on the clipboard: the message, and the trace under it when
        /// there is one, which is what a reader pastes into a bug report.</summary>
        private string CopyTextOf(ConsoleLog log)
        {
            if (string.IsNullOrEmpty(log.StackTrace)) return log.Message;

            return log.Message + "\n" + log.StackTrace;
        }

        /// <summary>How tall the whole list is, folded and filtered as it currently stands.</summary>
        private float ContentHeight()
        {
            if (_cumulativeHeights == null || _cachedVisibleLogs == null) return 0f;
            if (_cachedVisibleLogs.Count == 0) return 0f;

            return _cumulativeHeights[_cachedVisibleLogs.Count];
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