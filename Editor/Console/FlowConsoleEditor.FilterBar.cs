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

            // A ground of its own, darker than the list. Sharing the list's tone made the two read
            // as one surface, and the panel is a different thing on a different side.
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, FiltersPanelWidth, position.height),
                    FiltersPanelBackgroundColor);
            }

            // The switch that opened the panel, as the panel's own header. It is level with the
            // strip floating over the list, so the two read as one row across the window.
            FiltersToggleGUI(GUILayoutUtility.GetRect(FiltersPanelWidth, FloatingStripHeight,
                GUILayout.Width(FiltersPanelWidth), GUILayout.Height(FloatingStripHeight)));

            // The bar is drawn rather than taken from EditorStyles.toolbar: the toolbar's own
            // background is lighter than the panel under it, and it painted over the edge line
            // down the panel's left side.
            Rect barRect = GUILayoutUtility.GetRect(FiltersPanelWidth, FiltersPanelBarHeight,
                GUILayout.Width(FiltersPanelWidth), GUILayout.Height(FiltersPanelBarHeight));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(barRect, FiltersPanelBarColor);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1f, barRect.width, 1f),
                    FiltersPanelEdgeColor);
            }

            PresetMenuGUI(new Rect(barRect.xMax - 70f, barRect.y + 1f, 66f, barRect.height - 2f));

            _filtersPanelScroll = EditorGUILayout.BeginScrollView(_filtersPanelScroll,
                GUILayout.Width(FiltersPanelWidth));

            if (!string.IsNullOrEmpty(_isolatedChannel))
            {
                IsolatedChannelGUI();

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 1f, position.height),
                        FiltersPanelEdgeColor);
                }

                return;
            }

            // The project's own modules first, then the framework, then Unity. That is the order of
            // how much a reader cares: their game's lines are what they came to read, the
            // framework's are the machinery underneath them, and Unity's are the floor. The group
            // ids stay 0 Unity, 1 Framework, 2 Modules - they key the mute snapshot and the saved
            // state, and renumbering them would read somebody's saved panel back wrongly.
            CountChannels(logType => !logType.IsMandatory, out int moduleShown, out int moduleTotal);
            _moduleChannelsExpanded = FiltersGroupHeader("Modules", _moduleChannelsExpanded,
                moduleShown, moduleTotal, 2);

            if (_moduleChannelsExpanded)
                ModuleChannelRowsGUI();

            EditorGUILayout.Space(4f);

            CountChannels(logType => logType.IsMandatory && !IsUnityChannel(logType.Value),
                out int frameworkShown, out int frameworkTotal);
            _systemChannelsExpanded = FiltersGroupHeader("Framework", _systemChannelsExpanded,
                frameworkShown, frameworkTotal, 1);

            if (_systemChannelsExpanded)
                SystemChannelRowsGUI();

            EditorGUILayout.Space(4f);

            CountChannels(logType => IsUnityChannel(logType.Value), out int unityShown, out int unityTotal);
            _unityChannelsExpanded = FiltersGroupHeader("Unity", _unityChannelsExpanded, unityShown, unityTotal, 0);

            if (_unityChannelsExpanded)
                UnityChannelRowsGUI();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            // Last, so nothing the panel drew can paint over it.
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 1f, position.height), FiltersPanelEdgeColor);
        }

        /// <summary>
        /// A group's title, and how many of its channels are showing. The count is what a reader
        /// wants from a folded group - whether anything in there is hidden - so the group can stay
        /// folded and still answer it.
        /// </summary>
        private bool FiltersGroupHeader(string title, bool expanded, int shown, int total, int groupIndex)
        {
            // The whole row folds the group, not the arrow alone: a header that is a click target
            // for eight pixels of triangle is a header nobody hits first time.
            Rect rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight + 2f,
                GUILayout.ExpandWidth(true));

            bool hovered = rect.Contains(Event.current.mousePosition);

            // The header's ground is drawn rather than left to the foldout style, which paints only
            // as far as its own text: the count on the right sat on the panel's darker ground and
            // the row read as two colours. Drawn here, one band runs the width and lights whole.
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, hovered ? FiltersGroupHeaderHoverColor : FiltersGroupHeaderColor);
            }

            // The foldout is given everything but the mute button's corner. Handed the whole row it
            // took the click first - toggleOnLabelClick makes the rect the target - and pressing
            // Mute folded the group instead.
            var foldoutRect = new Rect(rect.x, rect.y, rect.width - GroupMuteWidth - 8f, rect.height);

            bool open = EditorGUI.Foldout(foldoutRect, expanded, title, true, EditorStyles.foldout);

            if (hovered && Event.current.type == EventType.MouseMove)
                Repaint();

            // Only on a repaint, because that is the pass EditorStyles is real in. Built during a
            // layout pass the style came back blank - no name, no font, black text, aligned to the
            // top left - which is what a style derived from a placeholder looks like.
            if (Event.current.type == EventType.Repaint)
            {
                EnsureChannelRowStyles();

                var countRect = new Rect(rect.xMax - GroupMuteWidth - 54f, rect.y, 46f, rect.height);

                GUI.Label(countRect, shown + " / " + total,
                    shown == total ? _groupCountStyle : _groupCountHighlightStyle);
            }

            GroupMuteGUI(new Rect(rect.xMax - GroupMuteWidth - 4f, rect.y + 2f, GroupMuteWidth,
                rect.height - 4f), groupIndex);


            if (open != expanded)
            {
                if (title == "Unity") _state.UnityChannelsExpanded = open;
                else if (title == "Framework") _state.FrameworkChannelsExpanded = open;
                else _state.ModuleChannelsExpanded = open;
            }

            return open;
        }

        /// <summary>
        /// What the panel shows while one channel is isolated: that channel, and the way out. The
        /// groups are put away because none of their switches mean anything while this is on, and
        /// a list of switches that do nothing is a list that lies.
        /// </summary>
        private void IsolatedChannelGUI()
        {
            if (!_settings.TryGetLogType(_isolatedChannel, out var typeInfo))
            {
                LeaveIsolation();
                return;
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Isolated", EditorStyles.miniLabel);

            ChannelRowGUI(typeInfo.Name, typeInfo, 0);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Exit isolation", EditorStyles.miniButton))
                LeaveIsolation();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Nothing was switched off to arrange this, so leaving it puts "
                                       + "every channel back the way you had it.", EditorStyles.wordWrappedMiniLabel);
        }

        private void EnterIsolation(string channel)
        {
            _isolatedChannel = channel;
            _state.IsolatedChannel = channel;
            OnLogTypeSelectionChanged();
        }

        private void LeaveIsolation()
        {
            _isolatedChannel = null;
            _state.IsolatedChannel = null;
            OnLogTypeSelectionChanged();
        }

        /// <summary>Which of the three groups a channel belongs to: 0 Unity, 1 Framework, 2 Modules.</summary>
        private static int GroupIndexOf(CD_FlowConsole.FlowConsoleLogTypeCVO logType)
        {
            if (IsUnityChannel(logType.Value)) return 0;

            return logType.IsMandatory ? 1 : 2;
        }

        /// <summary>
        /// Whether the group a channel belongs to is silenced. Asked while the list is filtered, so
        /// a muted group takes its channels off the list without any of them being switched off -
        /// which is what lets unmuting bring the reader's selection back exactly as it was.
        /// </summary>
        private bool IsGroupMuted(CD_FlowConsole.FlowConsoleLogTypeCVO logType)
        {
            return !_groupUnmuted[GroupIndexOf(logType)];
        }

        /// <summary>
        /// The switch at the end of a group's header. Clicking it silences the group; clicking it
        /// again brings back what was showing before, because nothing was switched off to begin
        /// with. Alt+clicking silences the other groups instead, and again brings them back - the
        /// same gesture a channel row has, one level up.
        /// </summary>
        private void GroupMuteGUI(Rect rect, int groupIndex)
        {
            bool muted = !_groupUnmuted[groupIndex];

            var content = new GUIContent(muted ? "Muted" : "Mute",
                "Silence this group without switching any of its channels off.\n"
                + "Alt+click to silence the others instead, and again to bring them back.");

            Color backgroundWas = GUI.backgroundColor;
            if (muted) GUI.backgroundColor = GroupMutedTintColor;

            bool pressed = GUI.Button(rect, content, EditorStyles.miniButton);

            GUI.backgroundColor = backgroundWas;

            if (!pressed) return;

            if (Event.current.alt)
            {
                // Coming out of a solo puts back what the reader had, not everything. They may have
                // arrived at the solo with a group already muted, and handing that back to them
                // switched on is losing a setting rather than restoring one.
                if (_soloedGroup == groupIndex)
                {
                    for (int i = 0; i < _groupUnmuted.Count; i++)
                        _groupUnmuted[i] = _groupUnmutedBeforeSolo[i];

                    _soloedGroup = -1;
                }
                else
                {
                    if (_soloedGroup < 0)
                    {
                        for (int i = 0; i < _groupUnmuted.Count; i++)
                            _groupUnmutedBeforeSolo[i] = _groupUnmuted[i];
                    }

                    for (int i = 0; i < _groupUnmuted.Count; i++)
                        _groupUnmuted[i] = i == groupIndex;

                    _soloedGroup = groupIndex;
                }
            }
            else
            {
                _groupUnmuted[groupIndex] = muted;
                _soloedGroup = -1;
            }

            _state.UnityMuted = !_groupUnmuted[0];
            _state.FrameworkMuted = !_groupUnmuted[1];
            _state.ModulesMuted = !_groupUnmuted[2];

            OnLogTypeSelectionChanged();
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
                if (_settings.IsLogTypeVisible(logType)) shown++;
            }
        }

        /// <summary>
        /// The channels that are not the framework narrating itself: what Unity wrote, what the
        /// compiler said, and what a shader would not compile into. They carry the output this
        /// window exists to take over from Unity's console, so they sit at the top of the panel
        /// rather than among the framework's own.
        /// </summary>
        private void UnityChannelRowsGUI()
        {
            int row = 0;

            for (int i = 0; i < UnityChannels.Length; i++)
            {
                SystemLogType channel = UnityChannels[i];
                if (!_settings.TryGetLogType((int) channel, out var typeInfo)) continue;

                ChannelRowGUI(channel.ToString(), typeInfo, row++);
            }
        }

        private static readonly SystemLogType[] UnityChannels =
            {SystemLogType.Unity, SystemLogType.Compiler, SystemLogType.Shader};

        /// <summary>
        /// Read off the list above rather than repeating it, so a channel added to the group is
        /// drawn there and skipped below without the two saying different things.
        /// </summary>
        private static bool IsUnityChannel(int value)
        {
            for (int i = 0; i < UnityChannels.Length; i++)
                if ((int) UnityChannels[i] == value)
                    return true;

            return false;
        }

        private void SystemChannelRowsGUI()
        {
            int row = 0;

            AllRowGUI("All", IsAllSystemTypesVisible(), visible =>
            {
                SetAllSystemTypesVisible(visible);
                OnLogTypeSelectionChanged();
            }, row++, !_groupUnmuted[1]);

            for (int i = 0; i < FrameworkChannelOrder.Length; i++)
            {
                SystemLogType channel = FrameworkChannelOrder[i];
                if (!_settings.TryGetLogType((int) channel, out var typeInfo)) continue;

                ChannelRowGUI(channel.ToString(), typeInfo, row++);
            }

            // Anything the list above has not heard of - a channel added to the enum and not to
            // the order - still appears, at the end, rather than quietly not being offered.
            for (int i = 0; i < SystemLogTypeValues.Length; i++)
            {
                SystemLogType channel = SystemLogTypeValues[i];
                if (channel == SystemLogType.All) continue;
                if (IsUnityChannel((int) channel)) continue;
                if (Array.IndexOf(FrameworkChannelOrder, channel) >= 0) continue;
                if (!_settings.TryGetLogType((int) channel, out var typeInfo)) continue;

                ChannelRowGUI(channel.ToString(), typeInfo, row++);
            }
        }

        /// <summary>
        /// The order the framework's channels are listed in, which is not the order their numbers
        /// happen to run in: SignalOperation is numbered last because it was added last, and it
        /// belongs beside Signal. A number is never changed to move a row - it is serialized into
        /// every settings asset already written.
        /// </summary>
        private static readonly SystemLogType[] FrameworkChannelOrder =
        {
            SystemLogType.Context,
            SystemLogType.Injection,
            SystemLogType.Signal,
            SystemLogType.SignalOperation,
            SystemLogType.Command,
            SystemLogType.CommandOperation,
            SystemLogType.Function,
            SystemLogType.Screen,
            SystemLogType.Pool,
            SystemLogType.Asset
        };

        private void ModuleChannelRowsGUI()
        {
            int row = 0;

            AllRowGUI("All", IsAllProjectTypesVisible(), SetAllProjectTypes, row++, !_groupUnmuted[2]);

            bool any = false;

            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.IsMandatory) continue;

                any = true;
                int value = logType.Value;
                ChannelRowGUI(logType.Name, logType, row++);
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
        private void ChannelRowGUI(string name, CD_FlowConsole.FlowConsoleLogTypeCVO logType, int rowIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, ChannelRowHeight, GUILayout.ExpandWidth(true));

            // A muted group is not a group whose switches are off - the settings are still there,
            // only silenced - so its rows are drawn dim and answer to nothing. Letting them be
            // clicked would change a setting the reader cannot see the effect of.
            bool muted = IsGroupMuted(logType);
            bool hovered = !muted && rect.Contains(Event.current.mousePosition);

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

                bool shown = _settings.IsLogTypeVisible(logType) && !muted;

                GUI.Label(labelRect, name, shown ? _channelOnStyle : _channelOffStyle);

                if (shown)
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

            // Alt+click isolates rather than switching everything else off. Nothing is written,
            // so leaving isolation needs no snapshot to put back.
            if (!string.IsNullOrEmpty(_isolatedChannel))
            {
                LeaveIsolation();
            }
            else if (Event.current.alt)
            {
                EnterIsolation(logType.Name);
            }
            else
            {
                _settings.Visibility.Show(logType, !_settings.IsLogTypeVisible(logType));
                OnLogTypeSelectionChanged();
            }

            Event.current.Use();
            Repaint();
        }

        /// <summary>
        /// The row that turns a whole group on or off. Drawn like the channels under it so the
        /// group reads as one list, and in bold so it is not mistaken for one of them.
        /// </summary>
        private void AllRowGUI(string label, bool allVisible, Action<bool> set, int rowIndex, bool muted)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, ChannelRowHeight, GUILayout.ExpandWidth(true));

            bool hovered = !muted && rect.Contains(Event.current.mousePosition);

            if (muted) allVisible = false;

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

        private bool IsAllProjectTypesVisible()
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (!logType.IsMandatory && !_settings.IsLogTypeVisible(logType)) return false;
            }

            return true;
        }

        private void SetAllProjectTypes(bool enabled)
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (!logType.IsMandatory)
                    _settings.Visibility.Show(logType, enabled);
            }

            OnLogTypeSelectionChanged();
        }

        private bool IsAllSystemTypesVisible()
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var lt = _settings.LogTypes[i];
                if (IsUnityChannel(lt.Value)) continue;
                if (lt.IsMandatory && lt.Value != (int) SystemLogType.All && !_settings.IsLogTypeVisible(lt))
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
                    _settings.Visibility.Show(lt, visible);
            }
        }
    }
}
#endif