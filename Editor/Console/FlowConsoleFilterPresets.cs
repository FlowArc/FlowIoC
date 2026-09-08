#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;

namespace FlowIoC.Editor.Console
{
    public class FilterPreset
    {
        public string Name;
        public List<int> VisibleChannels = new();
    }

    /// <summary>
    /// A set of channels somebody reaches for often, under a name. Two ship with the console and
    /// the rest are whatever a reader saves.
    ///
    /// Saved presets live in EditorPrefs rather than in CD_FlowConsole. That asset is committed,
    /// and one developer's filter has no business turning up in everybody else's diff.
    /// </summary>
    public class FlowConsoleFilterPresets
    {
        private const string PREFIX = "FlowIoC.Console.Preset.";
        private const string NAMES_KEY = "FlowIoC.Console.PresetNames";

        /// <summary>
        /// Named for the job rather than for the channels, because that is what somebody is
        /// looking for when they reach for one.
        /// </summary>
        public readonly List<FilterPreset> BuiltIn = new()
        {
            new FilterPreset
            {
                Name = "Signal chase",
                VisibleChannels = new List<int>
                {
                    (int) SystemLogType.Signal,
                    (int) SystemLogType.Command
                }
            },
            new FilterPreset
            {
                Name = "Screen debug",
                VisibleChannels = new List<int>
                {
                    (int) SystemLogType.Signal,
                    (int) SystemLogType.Screen
                }
            }
        };

        public void Save(string name, IReadOnlyCollection<int> visibleChannels)
        {
            if (string.IsNullOrEmpty(name)) return;

            var values = new List<string>();
            foreach (int channel in visibleChannels)
                values.Add(channel.ToString());

            EditorPrefs.SetString(PREFIX + name, string.Join(";", values));

            List<string> names = LoadNames();
            if (!names.Contains(name))
            {
                names.Add(name);
                EditorPrefs.SetString(NAMES_KEY, string.Join(";", names));
            }
        }

        public List<FilterPreset> LoadSaved()
        {
            var presets = new List<FilterPreset>();

            foreach (string name in LoadNames())
            {
                var preset = new FilterPreset {Name = name};

                foreach (string part in EditorPrefs.GetString(PREFIX + name, "").Split(';'))
                {
                    if (int.TryParse(part, out int channel))
                        preset.VisibleChannels.Add(channel);
                }

                presets.Add(preset);
            }

            return presets;
        }

        public void Delete(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            EditorPrefs.DeleteKey(PREFIX + name);

            List<string> names = LoadNames();
            if (names.Remove(name))
                EditorPrefs.SetString(NAMES_KEY, string.Join(";", names));
        }

        private static List<string> LoadNames()
        {
            var names = new List<string>();
            string stored = EditorPrefs.GetString(NAMES_KEY, "");

            if (string.IsNullOrEmpty(stored)) return names;

            foreach (string name in stored.Split(';'))
            {
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            return names;
        }
    }
}
#endif