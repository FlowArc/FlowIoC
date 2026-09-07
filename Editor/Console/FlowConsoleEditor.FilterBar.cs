#if UNITY_EDITOR
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
        private void TopPanelConsoleFilterGUI()
        {
            RebuildCachedLogs();

            EditorGUILayout.BeginHorizontal();

            bool prevLog = _logFilter[LogType.Log];
            bool prevWarn = _logFilter[LogType.Warning];
            bool prevErr = _logFilter[LogType.Error];

            _logFilter[LogType.Log] = EditorGUILayout.ToggleLeft("Log(" + _cachedLogCount + ")", _logFilter[LogType.Log], GUILayout.Width(75));

            GUI.backgroundColor = Color.yellow;
            _logFilter[LogType.Warning] =
                EditorGUILayout.ToggleLeft("War(" + _cachedWarningCount + ")", _logFilter[LogType.Warning], GUILayout.Width(75));

            GUI.backgroundColor = Color.red;
            _logFilter[LogType.Error] = EditorGUILayout.ToggleLeft("Err(" + _cachedErrorCount + ")", _logFilter[LogType.Error], GUILayout.Width(75));
            GUI.backgroundColor = Color.white;

            if (prevLog != _logFilter[LogType.Log] || prevWarn != _logFilter[LogType.Warning] || prevErr != _logFilter[LogType.Error])
            {
                _logsDirty = true;
                _needsRepaint = true;
            }

            GUILayout.FlexibleSpace();

            int selectedProjectCount = 0;
            if (_settings != null && _settings.LogTypes != null)
                for (int i = 0; i < _settings.LogTypes.Count; i++)
                    if (!_settings.LogTypes[i].IsMandatory && _settings.LogTypes[i].IsVisible)
                        selectedProjectCount++;

            string modulesLabel = selectedProjectCount > 0
                ? "Modules (" + selectedProjectCount + ")"
                : "Modules";

            GUI.backgroundColor = selectedProjectCount > 0 ? Color.cyan : Color.white;
            if (GUILayout.Button(modulesLabel, EditorStyles.miniButton, GUILayout.MinWidth(80)))
            {
                ShowProjectTypesMenu();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void ShowProjectTypesMenu()
        {
            var menu = new GenericMenu();

            if (_settings == null || _settings.LogTypes == null)
            {
                menu.AddDisabledItem(new GUIContent("No project log types found"));
                menu.ShowAsContext();
                return;
            }

            menu.AddItem(new GUIContent("Enable All"), false, () => SetAllProjectTypes(true));
            menu.AddItem(new GUIContent("Disable All"), false, () => SetAllProjectTypes(false));
            menu.AddSeparator("");

            bool hasItems = false;
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var logType = _settings.LogTypes[i];
                if (logType.IsMandatory) continue;

                hasItems = true;
                int value = logType.Value;

                menu.AddItem(
                    new GUIContent(logType.Name),
                    logType.IsVisible,
                    () => ToggleProjectType(value)
                );
            }

            if (!hasItems)
            {
                menu.AddSeparator("");
                menu.AddDisabledItem(new GUIContent("No project log types found"));
            }

            menu.ShowAsContext();
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

        private void ToggleProjectType(int typeValue)
        {
            if (_settings.TryGetLogType(typeValue, out var typeInfo))
            {
                typeInfo.IsVisible = !typeInfo.IsVisible;
                EditorUtility.SetDirty(_settings);
            }

            OnLogTypeSelectionChanged();
        }

        private bool IsAllSystemTypesVisible()
        {
            for (int i = 0; i < _settings.LogTypes.Count; i++)
            {
                var lt = _settings.LogTypes[i];
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
                if (lt.IsMandatory && lt.Value != (int) SystemLogType.All)
                    lt.IsVisible = visible;
            }

            EditorUtility.SetDirty(_settings);
        }

    }
}
#endif
