using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlowIoC.ConsoleModule
{
    public static class FlowLogTypeManager
    {
        #if UNITY_EDITOR
        public static void AddFlowLogTypesBatch(List<(string Name, int Value, Color LogColor)> newTypes)
        {
            if (newTypes == null || newTypes.Count == 0)
                return;

            var usedValues = new HashSet<int>();
            foreach (SystemLogType st in Enum.GetValues(typeof(SystemLogType)))
                usedValues.Add((int)st);

            var settings = FlowLogger.Settings;
            foreach (var lt in settings.LogTypes)
                usedValues.Add(lt.Value);

            const int autoBaseValue = 1000;
            const int autoStep = 10;

            int nextAutoValue = autoBaseValue;
            while (usedValues.Contains(nextAutoValue))
                nextAutoValue += autoStep;

            int updatedCount = 0;
            var resolved = new List<(string Name, int Value, Color LogColor)>();
            foreach (var entry in newTypes)
            {
                bool existsInSettings = false;
                foreach (var logType in settings.LogTypes)
                {
                    if (string.Equals(logType.Name, entry.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        existsInSettings = true;
                        if (!logType.IsAutoRegistered)
                        {
                            logType.IsAutoRegistered = true;
                            updatedCount++;
                        }
                        break;
                    }
                }
                if (existsInSettings) continue;

                int value;
                if (entry.Value >= 0)
                {
                    value = entry.Value;
                    while (usedValues.Contains(value))
                        value++;
                }
                else
                {
                    value = nextAutoValue;
                    while (usedValues.Contains(value))
                        value++;
                    nextAutoValue = value + autoStep;
                }
                usedValues.Add(value);

                resolved.Add((entry.Name, value, entry.LogColor));
            }

            if (resolved.Count == 0 && updatedCount == 0)
            {
                Debug.Log($"<color=cyan>FlowConsole:</color> All {newTypes.Count} module log type(s) are already up to date.");
                return;
            }

            if (resolved.Count == 0)
            {
                settings.SortProjectLogTypes();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                FlowConsoleSettings.NotifyProjectLogTypesChanged();
                Debug.Log($"<color=cyan>FlowConsole:</color> Updated {updatedCount} existing module log type(s).");
                return;
            }

            foreach (var entry in resolved)
            {
                var added = settings.AddLogType(entry.Name, entry.Value, entry.LogColor);
                if (added != null) added.IsAutoRegistered = true;
            }

            settings.SortProjectLogTypes();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            FlowConsoleSettings.NotifyProjectLogTypesChanged();

            string names = string.Join(", ", resolved.Select(r => r.Name));
            Debug.Log($"<color=cyan>FlowConsole:</color> Auto-registered {resolved.Count} module log type(s): {names}");
        }
        #endif
    }
}
