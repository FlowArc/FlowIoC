#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Which channels one developer has switched on or off in the Flow Console, on top of the
    /// defaults each channel ships with.
    ///
    /// It lives in EditorPrefs because somebody hiding Injection while they chase a signal is
    /// nobody else's business - committed, it would switch every other developer's channels to
    /// match when they pull. What the project agrees on is the default a channel carries: the
    /// framework's in <see cref="SystemLogChannelTable"/>, a module's on.
    ///
    /// Only the switches the developer threw are stored, as the channel's name and where they put
    /// it. A channel they never touched is not in the list, so it follows its default - and so
    /// does a channel a module added after they last opened the panel.
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

        public bool IsShown(FlowLogChannel channel)
        {
            return _switches.TryGetValue(channel.Name, out bool shown) ? shown : channel.IsVisibleByDefault;
        }

        public void Show(FlowLogChannel channel, bool shown)
        {
            if (shown == channel.IsVisibleByDefault)
                _switches.Remove(channel.Name);
            else
                _switches[channel.Name] = shown;

            Save();
        }

        /// <summary>Whether any channel stands somewhere other than its default.</summary>
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