#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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
        private const float PinIconSize = 16f;
        private const int LogTrimChunk = 256;

        private float[] _cachedLogHeights;
        private float[] _cumulativeHeights;

        private List<ConsoleLog> _multiTypeFilterBuffer;

        private int _layoutFirstVisible;
        private int _layoutLastVisible;

        private readonly FlowConsoleStickyTail _stickyTail = new FlowConsoleStickyTail();
        private readonly FlowConsoleKeyboard _keyboard = new FlowConsoleKeyboard();
        private readonly FlowConsoleSearch _search = new FlowConsoleSearch();
        private readonly FlowConsoleSolo _solo = new FlowConsoleSolo();
        private readonly FlowConsoleHighlight _highlight = new FlowConsoleHighlight();
        private readonly FlowConsoleTiming _timing = new FlowConsoleTiming();
        private bool _showTiming;

        private readonly FlowConsoleExport _export = new FlowConsoleExport();
        private readonly FlowConsoleFilterPresets _presets = new FlowConsoleFilterPresets();

        private bool _showFilters;
        private bool _showSettings;
        private GUIStyle _channelOnStyle;
        private GUIStyle _channelOffStyle;
        private GUIStyle _channelAllOnStyle;
        private GUIStyle _channelAllOffStyle;
        private GUIStyle _groupCountStyle;
        private GUIStyle _groupCountHighlightStyle;
        private const float ChannelRowHeight = 20f;
        private const float ChannelRowIndent = 14f;
        private const float ChannelSwatchSize = 9f;
        private static readonly Color ChannelRowBandColor = new Color(0f, 0f, 0f, 0.08f);
        private static readonly Color ChannelRowHoverColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color ChannelOnTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color ChannelOffTextColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color ChannelTickColor = new Color(0.35f, 0.8f, 0.4f, 1f);

        private bool _unityChannelsExpanded = true;
        private bool _systemChannelsExpanded = true;
        private bool _moduleChannelsExpanded = true;
        private Vector2 _filtersPanelScroll;
        private const float FiltersPanelWidth = 220f;
        private static readonly Color FiltersPanelEdgeColor = new Color(0f, 0f, 0f, 0.45f);

        private readonly FlowConsoleFlowTreeBuilder _flowTree = new FlowConsoleFlowTreeBuilder();
        private bool _flowMode;
        private readonly HashSet<int> _collapsedFlowIds = new();
        private readonly List<ConsoleLog> _flowRowBuffer = new();
        private readonly List<int> _flowDepthBuffer = new();
        private readonly List<int> _flowHeaderBuffer = new();
        private readonly List<int> _flowHiddenBuffer = new();
        private int[] _rowDepths;
        private int[] _rowFlowHeaders;
        private const float FlowHeaderHeight = 18f;
        private const float FlowIndent = 14f;
        private GUIStyle _flowHeaderStyle;
        private static readonly Color FlowHeaderColor = new Color(0.45f, 0.65f, 0.85f, 1f);

        private readonly FlowConsolePins _pins = new FlowConsolePins();
        private bool _pinnedOnly;


        private readonly FlowConsoleSessionRule _sessionRule = new FlowConsoleSessionRule();
        private GUIStyle _sessionSeparatorStyle;
        private const float SessionSeparatorHeight = 18f;
        private static readonly Color SessionSeparatorColor = new Color(0.78f, 0.78f, 0.78f, 0.9f);
        private static readonly Color SessionSeparatorBandColor = new Color(1f, 1f, 1f, 0.09f);

        /// <summary>Translucent, so the text keeps reading through it.</summary>
        private static readonly Color SearchHighlightColor = new Color(0.24f, 0.48f, 0.90f, 0.45f);

        private const string DimHex = "#A0A0A0";

        /// <summary>
        /// A dark cyan. The pin glyph is light, so the disc behind it has to be dark enough to
        /// leave the shape readable - yellow washed it out.
        /// </summary>
        private static readonly Color PinBadgeColor = new Color(0.05f, 0.42f, 0.48f, 1f);

        /// <summary>The same colour as a multiplier, for a control that paints its own background.</summary>
        private static readonly Color PinBadgeTintColor = new Color(0.35f, 1.1f, 1.25f, 1f);

        /// <summary>2 is a square with the corners taken off; half the width would be a disc.</summary>
        private const float PinBadgeCornerRadius = 2f;

        private const float PinBadgeSize = 15f;

        /// <summary>
        /// The plate behind the pin. Square with the corners just taken off, drawn through
        /// GUI.DrawTexture's border-radius overload because EditorGUI.DrawRect has square corners
        /// and nothing else.
        /// </summary>
        private static void DrawDisc(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, color,
                Vector4.zero, Vector4.one * PinBadgeCornerRadius);
        }

        private static string Dim(string text)
        {
            return "<color=" + DimHex + ">" + text + "</color>";
        }

        private FlowConsoleSearchQuery _searchQuery;

        /// <summary>Unity's own toolbar search field, which is where the cancel cross comes from.</summary>
        private SearchField _searchField;

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

            // Set on every enable rather than kept, because the texture behind an editor icon
            // belongs to the editor and is freed on a domain reload.
            titleContent = new GUIContent("Flow Console",
                EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow")?.image);

            // Without this the window is never told the pointer moved, and a toolbar button only
            // turns hovered on the next repaint something else happens to cause.
            wantsMouseMove = true;

            _rowLineCount = _state.RowLineCount;
            _collapseRows = _state.Collapse;
            _showTiming = _state.Timing;
            _flowMode = _state.FlowMode;
            _showFilters = _state.ShowFilters;
            _showSettings = _state.ShowSettings;
            _unityChannelsExpanded = _state.UnityChannelsExpanded;
            _systemChannelsExpanded = _state.FrameworkChannelsExpanded;
            _moduleChannelsExpanded = _state.ModuleChannelsExpanded;
            _searchQuery = _search.Parse(_searchText);

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

            // The list and the filters panel side by side. The panel's width is fixed, so what a
            // wider window buys is more room for the messages rather than a wider column of
            // channel names.
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            LogsPanelGUI();
            EditorGUILayout.EndVertical();

            if (_showFilters)
                FiltersPanelGUI();

            EditorGUILayout.EndHorizontal();

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
            // The severity counts are drawn up here now, so the list they count has to be rebuilt
            // before the toolbar rather than after it. The call does nothing unless it is due.
            RebuildCachedLogs();

            Rect toolbarRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Kept only from a repaint. BeginHorizontal answers with an empty rect during a
            // layout pass, and storing that wiped the real one just before the mouse-move event
            // that needed it - which is why hovering was quick coming from Unity's own controls
            // and slow everywhere else.
            if (Event.current.type == EventType.Repaint)
                _toolbarRect = toolbarRect;

            ClearButtonGUI();

            // Collapse before Error Pause, which is the order Unity's own console puts them in.
            bool collapse = GUILayout.Toggle(_collapseRows, "Collapse", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (collapse != _collapseRows)
            {
                _collapseRows = collapse;
                _state.Collapse = collapse;
                _logsDirty = true;
                _needsRepaint = true;
            }

            bool errorPause = GUILayout.Toggle(_state.ErrorPause, "Error Pause", EditorStyles.toolbarButton,
                GUILayout.Width(80));
            if (errorPause != _state.ErrorPause)
                _state.ErrorPause = errorPause;

            var flowLabel = new GUIContent("Flow",
                "Group the rows into the flows they belong to. A flow started from inside another sits under it.");

            bool flowMode = GUILayout.Toggle(_flowMode, flowLabel, EditorStyles.toolbarButton, GUILayout.Width(50));
            if (flowMode != _flowMode)
            {
                _flowMode = flowMode;
                _state.FlowMode = flowMode;
                _logsDirty = true;
                _needsRepaint = true;
            }

            // Fetched every draw, never kept: the texture behind an editor icon is freed on a
            // domain reload and a GUIContent holding one draws nothing.
            var pinnedLabel = new GUIContent(" Pinned", EditorGUIUtility.IconContent("pin")?.image,
                "Show only the rows you pinned. Pin one with the row's right-click menu, or with P.");

            // Tinted rather than drawn behind: a toolbar button paints its own pressed background
            // and covered anything under it. GUI.backgroundColor multiplies that background, so
            // the switch takes the same colour the pinned rows carry.
            Color backgroundWas = GUI.backgroundColor;
            if (_pinnedOnly) GUI.backgroundColor = PinBadgeTintColor;

            bool pinnedOnly = GUILayout.Toggle(_pinnedOnly, pinnedLabel, EditorStyles.toolbarButton,
                GUILayout.Width(76), GUILayout.ExpandHeight(true));

            GUI.backgroundColor = backgroundWas;
            if (pinnedOnly != _pinnedOnly)
            {
                _pinnedOnly = pinnedOnly;
                _logsDirty = true;
                _needsRepaint = true;
            }

            var timingLabel = new GUIContent("Timing",
                "Lead each row with the frame it was written in and the gap since the row above, instead of the clock.");

            bool timing = GUILayout.Toggle(_showTiming, timingLabel, EditorStyles.toolbarButton, GUILayout.Width(60));
            if (timing != _showTiming)
            {
                _showTiming = timing;
                _state.Timing = timing;
                _needsRepaint = true;
            }

            GUILayout.FlexibleSpace();

            _searchField ??= new SearchField();

            string newSearch = _searchField.OnToolbarGUI(_searchText, GUILayout.MinWidth(200));
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
                _searchQuery = _search.Parse(_searchText);
                _logsDirty = true;
                _needsRepaint = true;
            }

            // A half-typed expression matches nothing, and a list that empties for no visible
            // reason reads as a broken window. Say which it is.
            if (_searchQuery != null && _searchQuery.IsBrokenPattern)
            {
                GUILayout.Label("bad pattern", EditorStyles.miniLabel);
            }

            SeverityTogglesGUI();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// The three severity switches, drawn where Unity's console draws them: at the right end of
        /// the toolbar, as an icon and a count rather than a word and a number in brackets.
        /// </summary>
        private void SeverityTogglesGUI()
        {
            SeverityToggleGUI(LogType.Log, _cachedLogCount);
            SeverityToggleGUI(LogType.Warning, _cachedWarningCount);
            SeverityToggleGUI(LogType.Error, _cachedErrorCount);
        }

        private void SeverityToggleGUI(LogType logType, int count)
        {
            var content = new GUIContent(" " + count, IconFor(logType, true), logType + " messages");

            bool shown = GUILayout.Toggle(_logFilter[logType], content, EditorStyles.toolbarButton,
                GUILayout.MinWidth(38f), GUILayout.ExpandHeight(true));

            if (shown == _logFilter[logType]) return;

            _logFilter[logType] = shown;
            _logsDirty = true;
            _needsRepaint = true;
        }

        /// <summary>
        /// The sets of channels a reader keeps coming back to. Two ship with the console, named
        /// for the job rather than the channels, and the rest are whatever they save.
        /// </summary>
        private void PresetMenuGUI()
        {
            var content = new GUIContent("Presets", "Channel filters saved under a name.");
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, GUILayout.Width(66));

            if (!GUI.Button(rect, content, EditorStyles.toolbarDropDown)) return;

            var menu = new GenericMenu();

            for (int i = 0; i < _presets.BuiltIn.Count; i++)
            {
                FilterPreset preset = _presets.BuiltIn[i];
                menu.AddItem(new GUIContent(preset.Name), false, () => ApplyPreset(preset));
            }

            List<FilterPreset> saved = _presets.LoadSaved();

            if (saved.Count > 0)
            {
                menu.AddSeparator("");

                for (int i = 0; i < saved.Count; i++)
                {
                    FilterPreset preset = saved[i];
                    menu.AddItem(new GUIContent(preset.Name), false, () => ApplyPreset(preset));
                }

                menu.AddSeparator("");

                for (int i = 0; i < saved.Count; i++)
                {
                    string name = saved[i].Name;
                    menu.AddItem(new GUIContent("Delete/" + name), false, () => _presets.Delete(name));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Save shown channels..."), false, SaveCurrentPreset);

            menu.DropDown(new Rect(rect.x, rect.yMax, 0f, 0f));
        }

        private void ApplyPreset(FilterPreset preset)
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.Value == (int) SystemLogType.All) continue;

                logType.IsVisible = preset.VisibleChannels.Contains(logType.Value);
            }

            EditorUtility.SetDirty(_settings);
            OnLogTypeSelectionChanged();
        }

        private void SaveCurrentPreset()
        {
            var visible = new List<int>();

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.Value == (int) SystemLogType.All) continue;
                if (logType.IsVisible) visible.Add(logType.Value);
            }

            FlowConsolePresetNameWindow.Show(name => _presets.Save(name, visible));
        }

        /// <summary>
        /// Handing the list to somebody who was not at the machine. What it writes is what the
        /// console is showing - filters, search and Collapse included - because the rows the
        /// reader narrowed down to are the ones worth sending.
        /// </summary>
        private void ExportMenuGUI()
        {
            var content = new GUIContent("Export", "Save or copy the rows the console is showing.");
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, GUILayout.Width(60));

            if (!GUI.Button(rect, content, EditorStyles.toolbarDropDown)) return;

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Save shown rows..."), false, () => SaveShownLogs(false));
            menu.AddItem(new GUIContent("Save shown rows with stack traces..."), false, () => SaveShownLogs(true));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy shown rows"), false,
                () => EditorGUIUtility.systemCopyBuffer = _export.ToText(_cachedVisibleLogs, false));

            menu.DropDown(new Rect(rect.x, rect.yMax, 0f, 0f));
        }

        private void SaveShownLogs(bool includeStackTrace)
        {
            string suggested = "flow-console-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            string path = EditorUtility.SaveFilePanel("Export Flow Console", "", suggested, "txt");

            if (string.IsNullOrEmpty(path)) return;

            File.WriteAllText(path, _export.ToText(_cachedVisibleLogs, includeStackTrace));
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

        /// <summary>
        /// The bar under the toolbar. It carries the switch that opens the filters panel and the
        /// two things that say what the list is holding; the channels themselves live in the panel
        /// rather than in a horizontal strip, because thirty channels do not fit across a window
        /// and reading them meant scrolling sideways for a list that is read downwards.
        /// </summary>
        private void TopPanelGUI()
        {
            EditorGUILayout.BeginVertical();

            RebuildCachedLogs();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Only while something is selected, because that is the only time it can do anything.
            // A button that is there but greyed out asks the reader to work out why.
            if (_selectedLog != null)
            {
                var focusLabel = new GUIContent("Focus Log", "Scroll the selected row back into view.");

                if (GUILayout.Button(focusLabel, EditorStyles.toolbarButton, GUILayout.Width(70f),
                        GUILayout.ExpandHeight(true)))
                {
                    _scrollToSelectedLog = true;
                    _needsRepaint = true;
                }
            }

            // A search or a muted channel takes rows off the list and says so nowhere else. The
            // reader can see the scrollbar got shorter; this says by how much.
            int shown = _cachedVisibleLogs?.Count ?? 0;
            int held = _allLogs?.Count ?? 0;

            GUILayout.Label(
                new GUIContent(shown + " / " + held, "Rows on the list, and logs the console is holding."),
                shown == held ? EditorStyles.miniLabel : EditorStyles.whiteMiniLabel,
                GUILayout.Width(90f));

            GUILayout.FlexibleSpace();

            // Laid along this bar rather than in a panel of their own, because there are two of
            // them and they are read where the rows they shape are read.
            SettingsControlsGUI();

            var settingsLabel = SettingsButtonContent();

            bool showSettings = GUILayout.Toggle(_showSettings, settingsLabel, EditorStyles.toolbarButton,
                GUILayout.Width(30f), GUILayout.ExpandHeight(true));

            if (showSettings != _showSettings)
            {
                _showSettings = showSettings;
                _state.ShowSettings = showSettings;
                _needsRepaint = true;
            }

            var filtersLabel = new GUIContent("Filters",
                "Open the panel that holds every channel this console can show.");

            // As wide as the panel it opens and hard against the right edge, so it reads as that
            // panel's own header rather than as another toolbar button.
            bool showFilters = GUILayout.Toggle(_showFilters, filtersLabel, EditorStyles.toolbarButton,
                GUILayout.Width(FiltersPanelWidth), GUILayout.ExpandHeight(true));

            if (showFilters != _showFilters)
            {
                _showFilters = showFilters;
                _state.ShowFilters = showFilters;
                _needsRepaint = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Alt+clicking a channel narrows the console to it, and alt+clicking the one that is
        /// already alone brings the rest back. The All row is not a channel and takes no part.
        /// </summary>
        private void SoloSystemType(SystemLogType channel)
        {
            var channels = new List<CD_FlowConsole.FlowConsoleLogTypeCVO>();

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                if (_settings.LogTypes[i].Value == (int) SystemLogType.All) continue;
                channels.Add(_settings.LogTypes[i]);
            }

            var visible = new List<bool>(channels.Count);
            int index = -1;

            for (int i = 0; i < channels.Count; i++)
            {
                visible.Add(channels[i].IsVisible);
                if (channels[i].Value == (int) channel) index = i;
            }

            if (index < 0) return;

            _solo.Apply(visible, index);

            for (int i = 0; i < channels.Count; i++)
                channels[i].IsVisible = visible[i];

            EditorUtility.SetDirty(_settings);
            OnLogTypeSelectionChanged();
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

                    // A pin outranks a filter. Turning a channel off means hide that kind of log,
                    // and the reader already said this particular one is not the kind they meant.
                    if (log.Pinned)
                    {
                        _multiTypeFilterBuffer.Add(log);
                        continue;
                    }

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

            if (currentEvent.keyCode == KeyCode.P && !currentEvent.control && !currentEvent.command)
            {
                if (_selectedLog != null)
                {
                    _pins.Toggle(_selectedLog);
                    _logsDirty = true;
                }

                Repaint();
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
                if (_pinnedOnly && !log.Pinned) continue;

                // The severity toggles are a filter like the channels, so a pin outranks them too.
                // A search is not - that is the reader looking for something rather than hiding a
                // kind of log, and answering it with rows they pinned for another reason is noise.
                if (!log.Pinned && !_logFilter[log.LogType]) continue;
                if (hasSearch && !_searchQuery.Matches(log.Message)) continue;
                _cachedVisibleLogs.Add(log);
            }

            if (_flowMode)
                BuildFlowRows();
            else
                FoldVisibleLogs();

            RebuildLogHeights();
        }

        /// <summary>
        /// Regroups the rows into the flows they belong to. Read straight down, a busy frame is
        /// four operations interleaved; grouped, each one is a block a reader can follow, and a
        /// flow started from inside another sits indented under it.
        ///
        /// Collapse is not applied here. Folding equal rows and grouping them by the flow they
        /// came from answer different questions, and doing both leaves a count on a row whose
        /// neighbours came from somewhere else.
        /// </summary>
        private void BuildFlowRows()
        {
            List<FlowNode> roots = _flowTree.Build(_cachedVisibleLogs);

            _flowRowBuffer.Clear();
            _flowDepthBuffer.Clear();
            _flowHeaderBuffer.Clear();
            _flowHiddenBuffer.Clear();

            for (int i = 0; i < roots.Count; i++)
                FlattenFlow(roots[i], 0);

            _cachedVisibleLogs.Clear();
            _cachedVisibleLogs.AddRange(_flowRowBuffer);

            int count = _flowRowBuffer.Count;

            if (_rowDepths == null || _rowDepths.Length < count)
                _rowDepths = new int[Mathf.Max(count, 64)];
            if (_rowFlowHeaders == null || _rowFlowHeaders.Length < count)
                _rowFlowHeaders = new int[Mathf.Max(count, 64)];
            if (_collapseCounts == null || _collapseCounts.Length < count)
                _collapseCounts = new int[Mathf.Max(count, 64)];

            for (int i = 0; i < count; i++)
            {
                _rowDepths[i] = _flowDepthBuffer[i];
                _rowFlowHeaders[i] = _flowHeaderBuffer[i];

                // A collapsed flow keeps one row and says how many it stands for, which is what
                // the fold badge already means.
                _collapseCounts[i] = _flowHiddenBuffer[i];
            }
        }

        private void FlattenFlow(FlowNode node, int depth)
        {
            // A log outside any flow is a row of its own with nothing to head it.
            if (node.FlowId == 0)
            {
                for (int i = 0; i < node.Logs.Count; i++)
                {
                    _flowRowBuffer.Add(node.Logs[i]);
                    _flowDepthBuffer.Add(depth);
                    _flowHeaderBuffer.Add(0);
                    _flowHiddenBuffer.Add(1);
                }

                return;
            }

            bool collapsed = _collapsedFlowIds.Contains(node.FlowId);
            int shown = collapsed ? Mathf.Min(1, node.Logs.Count) : node.Logs.Count;

            for (int i = 0; i < shown; i++)
            {
                _flowRowBuffer.Add(node.Logs[i]);
                _flowDepthBuffer.Add(depth);
                _flowHeaderBuffer.Add(i == 0 ? node.FlowId : 0);
                _flowHiddenBuffer.Add(collapsed && i == 0 ? CountUnder(node) : 1);
            }

            if (collapsed) return;

            for (int i = 0; i < node.Children.Count; i++)
                FlattenFlow(node.Children[i], depth + 1);
        }

        /// <summary>How many rows a collapsed flow is standing in for, its children included.</summary>
        private int CountUnder(FlowNode node)
        {
            int count = node.Logs.Count;

            for (int i = 0; i < node.Children.Count; i++)
                count += CountUnder(node.Children[i]);

            return count;
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

        private int FlowHeaderAt(int rowIndex)
        {
            if (!_flowMode || _rowFlowHeaders == null) return 0;
            if (rowIndex < 0 || rowIndex >= _rowFlowHeaders.Length) return 0;

            return _rowFlowHeaders[rowIndex];
        }

        private int DepthAt(int rowIndex)
        {
            if (!_flowMode || _rowDepths == null) return 0;
            if (rowIndex < 0 || rowIndex >= _rowDepths.Length) return 0;

            return _rowDepths[rowIndex];
        }

        /// <summary>
        /// The line that opens a flow, with the arrow that folds it away. A flow is a signal and
        /// everything its commands wrote, so folding one is how a reader puts an operation they
        /// have already read out of the way without losing the rest of the list.
        /// </summary>
        private void FlowHeaderGUI(Rect rect, int flowId, int rowIndex)
        {
            bool collapsed = _collapsedFlowIds.Contains(flowId);
            float indent = DepthAt(rowIndex) * FlowIndent;
            var labelRect = new Rect(rect.x + indent + 6f, rect.y, rect.width - indent - 12f, rect.height);

            if (Event.current.type == EventType.Repaint)
            {
                EnsureFlowHeaderStyle();

                EditorGUI.DrawRect(new Rect(rect.x + indent, rect.y + rect.height - 1f,
                    rect.width - indent, 1f), new Color(0.4f, 0.4f, 0.4f, 0.6f));

                GUI.Label(labelRect, (collapsed ? "▸ " : "▾ ") + "Flow " + flowId, _flowHeaderStyle);
            }

            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown || !rect.Contains(currentEvent.mousePosition)) return;
            if (currentEvent.button != 0) return;

            if (!_collapsedFlowIds.Add(flowId))
                _collapsedFlowIds.Remove(flowId);

            _logsDirty = true;
            Repaint();
            currentEvent.Use();
        }

        private void EnsureFlowHeaderStyle()
        {
            if (_flowHeaderStyle != null) return;

            _flowHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                name = "FlowConsoleFlowHeader",
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            _flowHeaderStyle.normal.textColor = FlowHeaderColor;
        }

        /// <summary>
        /// The line between one session and the next: a rule across the list with the side it is
        /// opening written on it, so a play session can be told from the editing around it.
        /// </summary>
        private void DrawSessionSeparator(Rect rect, ConsoleLog opening)
        {
            if (Event.current.type != EventType.Repaint) return;

            EnsureSessionSeparatorStyle();

            string label = _sessionRule.Label(opening);
            var content = new GUIContent(label);
            float labelWidth = _sessionSeparatorStyle.CalcSize(content).x + 10f;

            // A band behind it. Drawn on nothing the strip read as a black gap in the list, which
            // is the one thing a boundary between two sessions should not look like.
            EditorGUI.DrawRect(rect, SessionSeparatorBandColor);

            float middle = rect.y + rect.height * 0.5f;
            float left = rect.x + 8f;
            float right = rect.xMax - 8f;
            float labelLeft = left + 12f;

            EditorGUI.DrawRect(new Rect(left, middle, 12f, 1f), SessionSeparatorColor);
            EditorGUI.DrawRect(new Rect(labelLeft + labelWidth, middle, Mathf.Max(0f, right - labelLeft - labelWidth),
                1f), SessionSeparatorColor);

            GUI.Label(new Rect(labelLeft + 5f, rect.y, labelWidth, rect.height), content, _sessionSeparatorStyle);
        }

        /// <summary>
        /// Built on every draw rather than kept. A style built during the first repaint after a
        /// domain reload can be a copy of a placeholder, and cached it stays a placeholder for the
        /// life of the window - which is how this label kept coming out black however it was
        /// written. There are only ever a handful of separators on screen.
        /// </summary>
        private void EnsureSessionSeparatorStyle()
        {
            _sessionSeparatorStyle = new GUIStyle(EditorStyles.label)
            {
                name = "FlowConsoleSessionSeparator",
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };

            _sessionSeparatorStyle.normal.textColor = SessionSeparatorColor;
        }

        /// <summary>
        /// Paints the part of a line that matched the search behind the text. A narrowed list says
        /// which rows survived but not why, and on a long message the word that matched can be
        /// anywhere.
        /// </summary>
        private void DrawSearchHighlight(Rect lineRect, string drawn)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (string.IsNullOrEmpty(_searchText) || _searchQuery == null) return;

            List<HighlightRange> ranges = _highlight.Ranges(drawn, _searchQuery);
            if (ranges.Count == 0) return;

            var content = new GUIContent(drawn);

            for (int i = 0; i < ranges.Count; i++)
            {
                Vector2 from = _richTextStyle.GetCursorPixelPosition(lineRect, content, ranges[i].Start);
                Vector2 to = _richTextStyle.GetCursorPixelPosition(
                    lineRect, content, ranges[i].Start + ranges[i].Length);

                // A match the row is too narrow to show, or one the label wrapped onto a line that
                // is not drawn. There is nothing to paint on this row.
                if (to.x <= from.x) continue;

                float width = Mathf.Min(to.x, lineRect.xMax) - from.x;
                if (width <= 0f) continue;

                EditorGUI.DrawRect(new Rect(from.x, lineRect.y + 1f, width, lineRect.height - 2f),
                    SearchHighlightColor);
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
                // A row that opens a play or edit session, or heads a flow, is taller by the line
                // drawn above it.
                ConsoleLog previous = i > 0 ? _cachedVisibleLogs[i - 1] : null;
                float height = rowHeight;

                if (_sessionRule.StartsSession(previous, _cachedVisibleLogs[i]))
                    height += SessionSeparatorHeight;

                if (FlowHeaderAt(i) != 0)
                    height += FlowHeaderHeight;

                _cachedLogHeights[i] = height;
                _cumulativeHeights[i + 1] = _cumulativeHeights[i] + height;
            }
        }

        private void LogGUI(ConsoleLog consoleLog, float entryHeight, int rowIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(0, entryHeight, GUILayout.ExpandWidth(true));

            ConsoleLog above = rowIndex > 0 && rowIndex - 1 < _cachedVisibleLogs.Count
                ? _cachedVisibleLogs[rowIndex - 1]
                : null;

            if (_sessionRule.StartsSession(above, consoleLog))
            {
                DrawSessionSeparator(new Rect(rect.x, rect.y, rect.width, SessionSeparatorHeight), consoleLog);
                rect = new Rect(rect.x, rect.y + SessionSeparatorHeight, rect.width,
                    rect.height - SessionSeparatorHeight);
            }

            int headerFlowId = FlowHeaderAt(rowIndex);

            if (headerFlowId != 0)
            {
                FlowHeaderGUI(new Rect(rect.x, rect.y, rect.width, FlowHeaderHeight), headerFlowId, rowIndex);
                rect = new Rect(rect.x, rect.y + FlowHeaderHeight, rect.width, rect.height - FlowHeaderHeight);
            }

            float indent = DepthAt(rowIndex) * FlowIndent;
            if (indent > 0f)
                rect = new Rect(rect.x + indent, rect.y, rect.width - indent, rect.height);

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
            float iconSize = small ? 16f : 32f;
            float iconTop = rect.y + (rect.height - iconSize) * 0.5f;

            if (icon != null)
                GUI.DrawTexture(new Rect(rect.x + 6f, iconTop, iconSize, iconSize), icon, ScaleMode.StretchToFill);

            float textLeft = rect.x + (small ? 26f : 42f);
            // Two forms of the same prefix: the plain one is what the row is measured with, and the
            // rich one is what is drawn. The second half of it - the milliseconds, or the gap since
            // the row above - is dimmed, because it is read only when the seconds are not enough.
            string date;
            string dateRich;

            if (_showTiming)
            {
                bool hasPrevious = rowIndex > 0 && rowIndex - 1 < _cachedVisibleLogs.Count;
                float previousRealtime = hasPrevious ? _cachedVisibleLogs[rowIndex - 1].Realtime : 0f;

                date = _timing.Prefix(consoleLog.Frame, consoleLog.Realtime, previousRealtime, hasPrevious);

                int gap = date.IndexOf("  ", StringComparison.Ordinal);
                dateRich = gap < 0 ? date : date.Substring(0, gap) + Dim(date.Substring(gap));
            }
            else
            {
                string clock = consoleLog.Hour.ToString("00") + ":" + consoleLog.Minute.ToString("00") + ":" +
                               consoleLog.Second.ToString("00");
                string milliseconds = ":" + consoleLog.Millisecond.ToString("000");

                date = clock + milliseconds;
                dateRich = clock + Dim(milliseconds);
            }

            float textWidth = rect.width - (textLeft - rect.x) - 4f;

            // The count of a folded row, right-aligned so the messages stay lined up under each
            // other. Drawn before the text so the text knows how much room it has left.
            // The same badge counts two things, one at a time: how many equal rows Collapse folded
            // into this one, or how many rows the folded flow it heads stands for.
            int foldedCount = (_collapseRows || _flowMode) && _collapseCounts != null && rowIndex < _collapseCounts.Length
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

                if (line != 0)
                {
                    DrawSearchHighlight(lineRect, text);
                    GUI.Label(lineRect, text, _richTextStyle);
                    continue;
                }

                // The prefix is its own label so the message can keep being measured as plain
                // text. A colour tag inside the drawn string would move every character along and
                // the search highlight is placed by character index.
                float prefixWidth = _richTextStyle.CalcSize(new GUIContent(date + " | ")).x;
                var prefixRect = new Rect(lineRect.x, lineRect.y, prefixWidth, lineRect.height);

                GUI.Label(prefixRect, dateRich + " | ", _richTextStyle);

                // The pin sits between the time and the message, which is where the eye already
                // travels along the row. Drawn at the size the texture was authored for - scaling
                // a 16 pixel icon is what made it come out ragged.
                float pinWidth = 0f;

                if (consoleLog.Pinned)
                {
                    Texture pin = EditorGUIUtility.IconContent("pin")?.image;

                    if (pin != null)
                    {
                        var pinRect = new Rect(lineRect.x + prefixWidth,
                            lineRect.y + (lineRect.height - PinIconSize) * 0.5f, PinIconSize, PinIconSize);

                        // A coloured plate behind it, so a pinned row is picked out of a moving
                        // list by colour rather than by recognising a small grey glyph. Inset,
                        // because the pin's own texture carries empty space around the glyph.
                        DrawDisc(new Rect(pinRect.x + 0.5f, pinRect.y - 0.5f, PinBadgeSize, PinBadgeSize),
                            PinBadgeColor);

                        GUI.DrawTexture(pinRect, pin, ScaleMode.StretchToFill);
                    }

                    pinWidth = PinIconSize + 3f;
                }

                var messageRect = new Rect(lineRect.x + prefixWidth + pinWidth, lineRect.y,
                    Mathf.Max(0f, lineRect.width - prefixWidth - pinWidth), lineRect.height);

                DrawSearchHighlight(messageRect, text);
                GUI.Label(messageRect, text, _richTextStyle);
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

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent(consoleLog.Pinned ? "Unpin" : "Pin"), consoleLog.Pinned, () =>
                    {
                        _pins.Toggle(consoleLog);
                        _logsDirty = true;
                        _needsRepaint = true;
                    });

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

            _pins.Trim(_allLogs, maxLogCount);
        }
    }
}
#endif