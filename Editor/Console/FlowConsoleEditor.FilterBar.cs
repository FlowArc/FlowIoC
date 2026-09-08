#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The severity counters, the Modules menu, and the switches that turn channels on and
    /// off. Split out of the window because filtering is its own job.
    /// </summary>
    internal partial class FlowConsoleEditor
    {
        /// <summary>
        /// The filters panel down the right-hand side: every channel the console can show, the
        /// framework's own above and the project's modules below, each group foldable. A channel
        /// list belongs in a column - there are thirty of them and their names are words, so a
        /// strip across the top could only be read by scrolling sideways.
        /// </summary>
        private void FiltersPanelGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(FiltersPanelWidth));

            Rect panelRect = GUILayoutUtility.GetRect(FiltersPanelWidth, 0f, GUILayout.Width(FiltersPanelWidth),
                GUILayout.Height(0f));

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 1f, position.height), FiltersPanelEdgeColor);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            PresetMenuGUI();
            EditorGUILayout.EndHorizontal();

            _filtersPanelScroll = EditorGUILayout.BeginScrollView(_filtersPanelScroll,
                GUILayout.Width(FiltersPanelWidth));

            // Unity's own output first, because it is what the reader came from: this window
            // replaces Unity's console, and the two channels that carry what Unity wrote are not
            // the framework narrating itself.
            CountChannels(logType => IsUnityChannel(logType.Value), out int unityShown, out int unityTotal);
            _unityChannelsExpanded = FiltersGroupHeader("Unity", _unityChannelsExpanded, unityShown, unityTotal);

            if (_unityChannelsExpanded)
                UnityChannelRowsGUI();

            EditorGUILayout.Space(4f);

            CountChannels(logType => logType.IsMandatory && !IsUnityChannel(logType.Value),
                out int frameworkShown, out int frameworkTotal);
            _systemChannelsExpanded = FiltersGroupHeader("Framework", _systemChannelsExpanded,
                frameworkShown, frameworkTotal);

            if (_systemChannelsExpanded)
                SystemChannelRowsGUI();

            EditorGUILayout.Space(4f);

            CountChannels(logType => !logType.IsMandatory, out int moduleShown, out int moduleTotal);
            _moduleChannelsExpanded = FiltersGroupHeader("Modules", _moduleChannelsExpanded,
                moduleShown, moduleTotal);

            if (_moduleChannelsExpanded)
                ModuleChannelRowsGUI();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// A group's title, and how many of its channels are showing. The count is what a reader
        /// wants from a folded group - whether anything in there is hidden - so the group can stay
        /// folded and still answer it.
        /// </summary>
        private bool FiltersGroupHeader(string title, bool expanded, int shown, int total)
        {
            // The whole row folds the group, not the arrow alone: a header that is a click target
            // for eight pixels of triangle is a header nobody hits first time.
            Rect rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight + 2f,
                GUILayout.ExpandWidth(true));

            bool open = EditorGUI.Foldout(rect, expanded, title, true, EditorStyles.foldoutHeader);

            // Only on a repaint, because that is the pass EditorStyles is real in. Built during a
            // layout pass the style came back blank - no name, no font, black text, aligned to the
            // top left - which is what a style derived from a placeholder looks like.
            if (Event.current.type == EventType.Repaint)
            {
                EnsureChannelRowStyles();

                var countRect = new Rect(rect.xMax - 54f, rect.y, 46f, rect.height);

                GUI.Label(countRect, shown + " / " + total,
                    shown == total ? _groupCountStyle : _groupCountHighlightStyle);
            }


            if (open != expanded)
            {
                if (title == "Unity") _state.UnityChannelsExpanded = open;
                else if (title == "Framework") _state.FrameworkChannelsExpanded = open;
                else _state.ModuleChannelsExpanded = open;
            }

            return open;
        }

        /// <summary>How many channels in a group are showing, and how many there are.</summary>
        private void CountChannels(Func<CD_FlowConsole.FlowConsoleLogTypeCVO, bool> belongs, out int shown,
            out int total)
        {
            shown = 0;
            total = 0;

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.Value == (int) SystemLogType.All) continue;
                if (!belongs(logType)) continue;

                total++;
                if (logType.IsVisible) shown++;
            }
        }

        /// <summary>
        /// The two channels that are not the framework narrating itself: what Unity wrote, and what
        /// the compiler said. They carry the output this window exists to take over from Unity's
        /// console, so they sit at the top of the panel rather than among the framework's own.
        /// </summary>
        private void UnityChannelRowsGUI()
        {
            int row = 0;

            for (int i = 0; i < UnityChannels.Length; i++)
            {
                SystemLogType channel = UnityChannels[i];
                if (!_settings.TryGetLogType((int) channel, out var typeInfo)) continue;

                ChannelRowGUI(channel.ToString(), typeInfo, () => SoloSystemType(channel), row++);
            }
        }

        private static readonly SystemLogType[] UnityChannels = {SystemLogType.Unity, SystemLogType.Compiler};

        private static bool IsUnityChannel(int value)
        {
            return value == (int) SystemLogType.Unity || value == (int) SystemLogType.Compiler;
        }

        private void SystemChannelRowsGUI()
        {
            int row = 0;

            AllRowGUI("All", IsAllSystemTypesVisible(), visible =>
            {
                SetAllSystemTypesVisible(visible);
                OnLogTypeSelectionChanged();
            }, row++);

            for (int i = 0; i < SystemLogTypeValues.Length; i++)
            {
                SystemLogType channel = SystemLogTypeValues[i];
                if (channel == SystemLogType.All) continue;
                if (IsUnityChannel((int) channel)) continue;
                if (!_settings.TryGetLogType((int) channel, out var typeInfo)) continue;

                ChannelRowGUI(channel.ToString(), typeInfo, () => SoloSystemType(channel), row++);
            }
        }

        private void ModuleChannelRowsGUI()
        {
            int row = 0;

            AllRowGUI("All", IsAllProjectTypesVisible(), SetAllProjectTypes, row++);

            bool any = false;

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.IsMandatory) continue;

                any = true;
                int value = logType.Value;
                ChannelRowGUI(logType.Name, logType, () => SoloProjectType(value), row++);
            }

            if (any) return;

            EditorGUILayout.LabelField("No module channels yet.", EditorStyles.miniLabel);
        }

        /// <summary>
        /// One channel: its colour, its name, and a tick when it is showing. The row itself is the
        /// switch - a checkbox beside a name that is already a click target says the same thing
        /// twice, and a column of thirty of them reads as a form rather than a list. Alt+clicking
        /// narrows the console to that channel and alt+clicking the one that is already alone
        /// brings the rest back.
        /// </summary>
        private void ChannelRowGUI(string name, CD_FlowConsole.FlowConsoleLogTypeCVO logType, Action solo,
            int rowIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, ChannelRowHeight, GUILayout.ExpandWidth(true));

            bool hovered = rect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                // The same banding the log list uses, for the same reason: thirty rows of words
                // need something to keep the eye on one line.
                if (rowIndex % 2 == 1)
                    EditorGUI.DrawRect(rect, ChannelRowBandColor);

                if (hovered)
                    EditorGUI.DrawRect(rect, ChannelRowHoverColor);

                var swatch = new Rect(rect.x + ChannelRowIndent,
                    rect.y + (rect.height - ChannelSwatchSize) * 0.5f, ChannelSwatchSize, ChannelSwatchSize);

                EditorGUI.DrawRect(swatch, logType.LogColor);

                EnsureChannelRowStyles();

                var labelRect = new Rect(ChannelLabelLeft(rect), rect.y,
                    rect.width - (ChannelLabelLeft(rect) - rect.x) - 22f, rect.height);

                GUI.Label(labelRect, name, logType.IsVisible ? _channelOnStyle : _channelOffStyle);

                if (logType.IsVisible)
                {
                    Texture tick = EditorGUIUtility.IconContent("Valid")?.image;
                    var tickRect = new Rect(rect.xMax - 18f, rect.y + (rect.height - 14f) * 0.5f, 14f, 14f);

                    if (tick != null) GUI.DrawTexture(tickRect, tick, ScaleMode.ScaleToFit);
                    else EditorGUI.DrawRect(tickRect, ChannelTickColor);
                }
            }


            if (hovered && Event.current.type == EventType.MouseMove)
                Repaint();

            if (Event.current.type != EventType.MouseDown || !hovered || Event.current.button != 0) return;

            if (Event.current.alt)
            {
                solo();
            }
            else
            {
                logType.IsVisible = !logType.IsVisible;
                EditorUtility.SetDirty(_settings);
                OnLogTypeSelectionChanged();
            }

            Event.current.Use();
            Repaint();
        }

        /// <summary>
        /// The row that turns a whole group on or off. Drawn like the channels under it so the
        /// group reads as one list, and in bold so it is not mistaken for one of them.
        /// </summary>
        private void AllRowGUI(string label, bool allVisible, Action<bool> set, int rowIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, ChannelRowHeight, GUILayout.ExpandWidth(true));

            bool hovered = rect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (rowIndex % 2 == 1)
                    EditorGUI.DrawRect(rect, ChannelRowBandColor);

                if (hovered)
                    EditorGUI.DrawRect(rect, ChannelRowHoverColor);

                EnsureChannelRowStyles();

                var labelRect = new Rect(ChannelLabelLeft(rect), rect.y,
                    rect.width - (ChannelLabelLeft(rect) - rect.x) - 22f, rect.height);

                GUI.Label(labelRect, label, allVisible ? _channelAllOnStyle : _channelAllOffStyle);

                if (allVisible)
                {
                    Texture tick = EditorGUIUtility.IconContent("Valid")?.image;
                    var tickRect = new Rect(rect.xMax - 18f, rect.y + (rect.height - 14f) * 0.5f, 14f, 14f);

                    if (tick != null) GUI.DrawTexture(tickRect, tick, ScaleMode.ScaleToFit);
                    else EditorGUI.DrawRect(tickRect, ChannelTickColor);
                }
            }


            if (hovered && Event.current.type == EventType.MouseMove)
                Repaint();

            if (Event.current.type != EventType.MouseDown || !hovered || Event.current.button != 0) return;

            set(!allVisible);
            Event.current.Use();
            Repaint();
        }

        /// <summary>
        /// Where a channel's name starts. Indented past the group headers, so a row reads as
        /// something under the group rather than as another group.
        /// </summary>
        private static float ChannelLabelLeft(Rect rect)
        {
            return rect.x + ChannelRowIndent + ChannelSwatchSize + 6f;
        }

        /// <summary>
        /// Every style the panel draws with, each built only if it is missing.
        ///
        /// One guard for the whole set is what caused a storm of NullReferenceExceptions inside
        /// OnGUI: adding a style to the end of the method left the first one already built, so the
        /// new fields stayed null and the guard said there was nothing to do. Thrown from a repaint
        /// it also unbalanced GUILayout, which is the second error in every such storm.
        /// </summary>
        private void EnsureChannelRowStyles()
        {
            if (_channelOnStyle == null)
            {
                _channelOnStyle = new GUIStyle(EditorStyles.label)
                {
                    name = "FlowConsoleChannelOn",
                    alignment = TextAnchor.MiddleLeft
                };

                _channelOnStyle.normal.textColor = ChannelOnTextColor;
            }

            if (_channelOffStyle == null)
            {
                _channelOffStyle = new GUIStyle(_channelOnStyle) {name = "FlowConsoleChannelOff"};
                _channelOffStyle.normal.textColor = ChannelOffTextColor;
            }

            if (_channelAllOnStyle == null)
            {
                _channelAllOnStyle = new GUIStyle(_channelOnStyle)
                {
                    name = "FlowConsoleChannelAllOn",
                    fontStyle = FontStyle.Bold
                };
            }

            if (_channelAllOffStyle == null)
            {
                _channelAllOffStyle = new GUIStyle(_channelOffStyle)
                {
                    name = "FlowConsoleChannelAllOff",
                    fontStyle = FontStyle.Bold
                };
            }

            // Right-aligned, so the counts line up under one another however many digits they run
            // to - a column of numbers is read down its right edge.
            // Derived from the row style rather than from EditorStyles.miniLabel, which came back
            // as a placeholder and left the count black, full size and aligned top left.
            if (_groupCountStyle == null)
            {
                _groupCountStyle = new GUIStyle(_channelOffStyle)
                {
                    name = "FlowConsoleGroupCount",
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 10
                };
            }

            if (_groupCountHighlightStyle != null) return;

            _groupCountHighlightStyle = new GUIStyle(_groupCountStyle) {name = "FlowConsoleGroupCountHighlight"};
            _groupCountHighlightStyle.normal.textColor = ChannelOnTextColor;
        }

        /// <summary>
        /// The module channels' half of solo. It is a separate method from the framework's because
        /// the two groups are told apart by IsMandatory, and soloing a module means darkening the
        /// other modules rather than the whole list.
        /// </summary>
        private void SoloProjectType(int typeValue)
        {
            var channels = new List<CD_FlowConsole.FlowConsoleLogTypeCVO>();

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                if (_settings.LogTypes[i].IsMandatory) continue;
                channels.Add(_settings.LogTypes[i]);
            }

            var visible = new List<bool>(channels.Count);
            int index = -1;

            for (int i = 0; i < channels.Count; i++)
            {
                visible.Add(channels[i].IsVisible);
                if (channels[i].Value == typeValue) index = i;
            }

            if (index < 0) return;

            _solo.Apply(visible, index);

            for (int i = 0; i < channels.Count; i++)
                channels[i].IsVisible = visible[i];

            EditorUtility.SetDirty(_settings);
            OnLogTypeSelectionChanged();
        }

        private bool IsAllProjectTypesVisible()
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (!logType.IsMandatory && !logType.IsVisible) return false;
            }

            return true;
        }

        private void SetAllProjectTypes(bool enabled)
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (!logType.IsMandatory)
                    logType.IsVisible = enabled;
            }

            EditorUtility.SetDirty(_settings);
            OnLogTypeSelectionChanged();
        }

        private bool IsAllSystemTypesVisible()
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var lt = _settings.LogTypes[i];
                if (IsUnityChannel(lt.Value)) continue;
                if (lt.IsMandatory && lt.Value != (int) SystemLogType.All && !lt.IsVisible)
                    return false;
            }

            return true;
        }

        private void SetAllSystemTypesVisible(bool visible)
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var lt = _settings.LogTypes[i];
                if (IsUnityChannel(lt.Value)) continue;
                if (lt.IsMandatory && lt.Value != (int) SystemLogType.All)
                    lt.IsVisible = visible;
            }

            EditorUtility.SetDirty(_settings);
        }
    }
}
#endif