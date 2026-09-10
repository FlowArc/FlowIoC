#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Which channels one developer has switched on or off in the Flow Console, on top of the
    /// project defaults the settings asset carries.
    ///
    /// It lives in EditorPrefs rather than in CD_FlowConsole because that asset is committed, and
    /// somebody hiding Injection while they chase a signal is nobody else's business - pulled, it
    /// would switch every other developer's channels to match. The asset keeps only what the
    /// project agrees on: which channels are on for somebody who has not touched them.
    ///
    /// Only the switches the developer threw are stored, as the channel's name and where they put
    /// it. A channel they never touched is not in the list, so it follows the project default -
    /// and so does a channel a module added after they last opened the panel.
    /// </summary>
    public class FlowConsoleChannelVisibility
    {
        private readonly string _key;
        private readonly Dictionary<string, bool> _switches = new(StringComparer.OrdinalIgnoreCase);

        public FlowConsoleChannelVisibility(string key)
        {
            _key = key;
            Load();
        }

        public bool IsShown(CD_FlowConsole.FlowConsoleLogTypeCVO logType)
        {
            return _switches.TryGetValue(logType.Name, out bool shown) ? shown : logType.IsVisibleByDefault;
        }

        public void Show(CD_FlowConsole.FlowConsoleLogTypeCVO logType, bool shown)
        {
            if (shown == logType.IsVisibleByDefault)
                _switches.Remove(logType.Name);
            else
                _switches[logType.Name] = shown;

            Save();
        }

        /// <summary>Whether any channel stands somewhere other than its project default.</summary>
        public bool HasSwitches => _switches.Count > 0;

        public void Reset()
        {
            _switches.Clear();
            EditorPrefs.DeleteKey(_key);
        }

        private void Load()
        {
            foreach (string entry in EditorPrefs.GetString(_key, "").Split(';'))
            {
                int separator = entry.LastIndexOf('=');
                if (separator <= 0) continue;

                _switches[entry.Substring(0, separator)] = entry.Substring(separator + 1) == "1";
            }
        }

        private void Save()
        {
            // No switches is no key, the same as never having thrown one.
            if (_switches.Count == 0)
            {
                EditorPrefs.DeleteKey(_key);
                return;
            }

            var builder = new StringBuilder();

            foreach (KeyValuePair<string, bool> entry in _switches)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append(entry.Key).Append('=').Append(entry.Value ? '1' : '0');
            }

            EditorPrefs.SetString(_key, builder.ToString());
        }
    }
}
#endif