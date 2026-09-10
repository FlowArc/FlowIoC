#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The console's detail panel: the selected log's message, its source, and the stack
    /// trace with its clickable frames. Split out of the window so the list and the panel
    /// can be read apart from each other.
    /// </summary>
    internal partial class FlowConsoleEditor
    {
        private void DetailPanelGUI()
        {
            if (_selectedLog != null)
            {
                Rect splitterRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(8), GUILayout.ExpandWidth(true));
                Rect lineRect = new Rect(splitterRect.x, splitterRect.y + 3, splitterRect.width, 2);
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1f));
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

                Event e = Event.current;
                if (e.type == EventType.MouseDown && splitterRect.Contains(e.mousePosition))
                {
                    _isResizingDetailPanel = true;
                    e.Use();
                }

                if (e.type == EventType.MouseDrag && _isResizingDetailPanel)
                {
                    _detailPanelHeight -= e.delta.y;
                    _detailPanelHeight = Mathf.Clamp(_detailPanelHeight, 80f, position.height - 200f);
                    Repaint();
                    e.Use();
                }

                if (e.type == EventType.MouseUp && _isResizingDetailPanel)
                {
                    _isResizingDetailPanel = false;
                }

                // Only a vertical scrollbar. Everything in the panel wraps to its width, the way
                // Unity's console folds a long line, so there is nothing to scroll sideways to.
                _detailPanelScroll = EditorGUILayout.BeginScrollView(
                    _detailPanelScroll, false, false,
                    GUIStyle.none, GUI.skin.verticalScrollbar, _detailPanelStyle,
                    GUILayout.Height(_detailPanelHeight));

                GUILayout.Label(_selectedLog.Message, _detailRichTextStyle);
                EditorGUILayout.Space(4);

                if (_settings != null && _settings.DeepAnalysis)
                {
                    bool hasClassInfo = !string.IsNullOrEmpty(_selectedLog.SourceClassName);
                    bool hasFileInfo = !string.IsNullOrEmpty(_selectedLog.SourceFilePath);

                    if (hasClassInfo)
                        EditorGUILayout.LabelField("Class:", _selectedLog.SourceClassName);
                    if (hasFileInfo)
                        EditorGUILayout.LabelField("Line:", _selectedLog.SourceLineNumber.ToString());

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Stack Trace:", EditorStyles.boldLabel);

                    if (!string.IsNullOrEmpty(_selectedLog.StackTrace))
                    {
                        RebuildTraceCacheIfNeeded(_selectedLog);
                        for (int i = 0; i < _cachedTraceLines.Length; i++)
                        {
                            if (string.IsNullOrEmpty(_cachedTraceLines[i])) continue;

                            if (_cachedTraceFilePaths[i] != null)
                                DrawCachedClickableTrace(_cachedTraceDisplayTexts[i], _cachedTraceFilePaths[i], _cachedTraceLineNumbers[i],
                                    _cachedTraceClassNames[i]);
                            else
                                DrawNonClickableTraceLine(_cachedTraceDisplayTexts[i]);
                        }
                    }
                }
                else
                {
                    DrawSourceLink(_selectedLog);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void TryOpenSourceFile(ConsoleLog log)
        {
            _selectedLog = log;
            _navigator.Open(log);
        }

        private void InvalidateTraceCache()
        {
            _cachedTraceLog = null;
            _cachedTraceLines = null;
            _cachedTraceDisplayTexts = null;
            _cachedTraceFilePaths = null;
            _cachedTraceClassNames = null;
            _cachedTraceLineNumbers = null;
        }

        private void RebuildTraceCacheIfNeeded(ConsoleLog log)
        {
            if (_cachedTraceLog == log) return;
            _cachedTraceLog = log;

            _cachedTraceLines = log.StackTrace.Split('\n');
            int count = _cachedTraceLines.Length;
            _cachedTraceDisplayTexts = new string[count];
            _cachedTraceFilePaths = new string[count];
            _cachedTraceClassNames = new string[count];
            _cachedTraceLineNumbers = new int[count];

            for (int i = 0; i < count; i++)
            {
                string line = _cachedTraceLines[i];
                _cachedTraceDisplayTexts[i] = line;
                _cachedTraceFilePaths[i] = null;
                _cachedTraceClassNames[i] = null;
                _cachedTraceLineNumbers[i] = 0;

                if (string.IsNullOrEmpty(line)) continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    string fullClassName = line.Substring(0, colonIdx);
                    int slashIdx = fullClassName.IndexOf('/');
                    _cachedTraceClassNames[i] = slashIdx > 0
                        ? fullClassName.Substring(0, slashIdx)
                        : fullClassName;
                }

                int atIdx = line.LastIndexOf("(at ", StringComparison.Ordinal);
                if (atIdx < 0) continue;

                int closeIdx = line.LastIndexOf(')');
                if (closeIdx <= atIdx) continue;

                string atContent = line.Substring(atIdx + 4, closeIdx - atIdx - 4);
                int lastColon = atContent.LastIndexOf(':');
                if (lastColon < 0) continue;

                string pathPart = atContent.Substring(0, lastColon);
                string linePart = atContent.Substring(lastColon + 1);

                if (!int.TryParse(linePart, out int lineNumber)) continue;

                _cachedTraceFilePaths[i] = pathPart;
                _cachedTraceLineNumbers[i] = lineNumber;
                string shortName = PathText.FileNameOf(pathPart);
                _cachedTraceDisplayTexts[i] = line.Substring(0, atIdx) + $"(at {shortName}:{lineNumber})";
            }
        }

        private void DrawCachedClickableTrace(string displayText, string filePath, int lineNumber, string className)
        {
            var content = new GUIContent(displayText, filePath);
            Rect rect = GUILayoutUtility.GetRect(content, _linkStyle);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            GUI.Label(rect, content, _linkStyle);

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    _navigator.Open(filePath, lineNumber, className);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    ShowTraceLineContextMenu(displayText, filePath);
                    Event.current.Use();
                }
            }
        }

        private void DrawNonClickableTraceLine(string displayText)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(displayText), EditorStyles.wordWrappedLabel);
            GUI.Label(rect, displayText, EditorStyles.wordWrappedLabel);

            if (rect.Contains(Event.current.mousePosition) &&
                Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                ShowTraceLineContextMenu(displayText, null);
                Event.current.Use();
            }
        }

        private void ShowTraceLineContextMenu(string lineText, string filePath)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Line"), false, () => EditorGUIUtility.systemCopyBuffer = lineText);
            if (!string.IsNullOrEmpty(filePath))
                menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = filePath);
            if (_selectedLog != null && !string.IsNullOrEmpty(_selectedLog.StackTrace))
                menu.AddItem(new GUIContent("Copy Stack Trace"), false, () => EditorGUIUtility.systemCopyBuffer = _selectedLog.StackTrace);
            menu.ShowAsContext();
        }

        private void DrawSourceLink(ConsoleLog log)
        {
            string displayText = !string.IsNullOrEmpty(log.SourceTrace)
                ? log.SourceTrace
                : "Source information not available.";

            bool hasFileInfo = !string.IsNullOrEmpty(log.SourceFilePath);
            bool hasClassInfo = !string.IsNullOrEmpty(log.SourceClassName);

            if (!hasFileInfo && !hasClassInfo)
            {
                EditorGUILayout.LabelField(displayText);
                return;
            }

            string tooltip = hasFileInfo ? log.SourceFilePath : log.SourceClassName;
            var content = new GUIContent(displayText, tooltip);
            Rect rect = GUILayoutUtility.GetRect(content, _linkStyle);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            GUI.Label(rect, content, _linkStyle);

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    TryOpenSourceFile(log);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Copy"), false, () => EditorGUIUtility.systemCopyBuffer = displayText);
                    if (hasFileInfo)
                        menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = log.SourceFilePath);
                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }
        }
    }
}
#endif