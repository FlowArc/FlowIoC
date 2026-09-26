#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// How often this developer opened each scene from the switcher, and when last, which tab they
    /// left it on and which module groups they left open. Kept in EditorPrefs because it is one person's habit and not the
    /// project's, and keyed by the project, because two projects on one machine each have a
    /// MainScene.
    /// </summary>
    public class SceneSwitcherUsage
    {
        private const char ENTRY_SEPARATOR = '\n';
        private const char FIELD_SEPARATOR = '|';

        /// <summary>A scene nobody has opened for this many entries' worth of newer ones is forgotten.</summary>
        private const int CAPACITY = 40;

        private readonly string _usageKey = "FlowIoC.SceneSwitcher.Usage." + PlayerSettings.productGUID;
        private readonly string _tabKey = "FlowIoC.SceneSwitcher.Tab." + PlayerSettings.productGUID;
        private readonly string _expandedKey = "FlowIoC.SceneSwitcher.Expanded." + PlayerSettings.productGUID;

        public SceneSwitcherTab Tab
        {
            get => (SceneSwitcherTab) EditorPrefs.GetInt(_tabKey, (int) SceneSwitcherTab.Frequent);
            set => EditorPrefs.SetInt(_tabKey, (int) value);
        }

        /// <summary>Whether the module's group is left open in the Modules tab. Closed until opened.</summary>
        public bool IsExpanded(string module) => Expanded().Contains(module);

        public void SetExpanded(string module, bool expanded)
        {
            List<string> modules = Expanded();

            modules.Remove(module);
            if (expanded) modules.Add(module);

            EditorPrefs.SetString(_expandedKey, string.Join(FIELD_SEPARATOR, modules));
        }

        private List<string> Expanded()
        {
            string stored = EditorPrefs.GetString(_expandedKey, string.Empty);

            return new List<string>(stored.Split(FIELD_SEPARATOR, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>The scenes opened most, most first; a tie goes to the one opened last.</summary>
        public IReadOnlyList<string> MostUsed(int count)
        {
            List<Usage> usages = Read();

            usages.Sort((left, right) => left.Count != right.Count
                ? right.Count.CompareTo(left.Count)
                : right.LastOpened.CompareTo(left.LastOpened));

            var paths = new List<string>();

            for (int i = 0; i < usages.Count && i < count; i++)
                paths.Add(usages[i].Path);

            return paths;
        }

        public void Record(string scenePath)
        {
            List<Usage> usages = Read();
            int index = usages.FindIndex(usage => usage.Path == scenePath);
            int count = index >= 0 ? usages[index].Count + 1 : 1;

            if (index >= 0) usages.RemoveAt(index);

            usages.Add(new Usage(scenePath, count, DateTime.UtcNow.Ticks));

            // The oldest go first, so a scene used heavily a month ago gives way in the end.
            usages.Sort((left, right) => right.LastOpened.CompareTo(left.LastOpened));

            if (usages.Count > CAPACITY)
                usages.RemoveRange(CAPACITY, usages.Count - CAPACITY);

            Write(usages);
        }

        private List<Usage> Read()
        {
            var usages = new List<Usage>();
            string stored = EditorPrefs.GetString(_usageKey, string.Empty);

            foreach (string line in stored.Split(ENTRY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = line.Split(FIELD_SEPARATOR);

                if (fields.Length != 3) continue;
                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)) continue;
                if (!long.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)) continue;

                usages.Add(new Usage(fields[2], count, ticks));
            }

            return usages;
        }

        private void Write(List<Usage> usages)
        {
            var lines = new List<string>();

            foreach (Usage usage in usages)
            {
                lines.Add(usage.Count.ToString(CultureInfo.InvariantCulture) + FIELD_SEPARATOR +
                          usage.LastOpened.ToString(CultureInfo.InvariantCulture) + FIELD_SEPARATOR + usage.Path);
            }

            EditorPrefs.SetString(_usageKey, string.Join(ENTRY_SEPARATOR, lines));
        }

        private readonly struct Usage
        {
            public readonly string Path;
            public readonly int Count;
            public readonly long LastOpened;

            public Usage(string path, int count, long lastOpened)
            {
                Path = path;
                Count = count;
                LastOpened = lastOpened;
            }
        }
    }
}

#endif
