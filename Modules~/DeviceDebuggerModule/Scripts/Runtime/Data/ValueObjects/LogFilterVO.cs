using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>What the Console tab shows: the three kinds, and a text the row has to contain.</summary>
    public class LogFilterVO
    {
        public bool ShowLogs = true;
        public bool ShowWarnings = true;
        public bool ShowErrors = true;
        public string Search = "";

        /// <summary>Channels switched off in the Filters panel, by name - the Flow Console's switches, on the device.</summary>
        public HashSet<string> HiddenChannels = new();

        public bool Matches(ConsoleLog log)
        {
            if (log == null) return false;

            switch (log.LogType)
            {
                case LogType.Log:
                    if (!ShowLogs) return false;
                    break;
                case LogType.Warning:
                    if (!ShowWarnings) return false;
                    break;
                default:
                    if (!ShowErrors) return false;
                    break;
            }

            if (HiddenChannels.Count > 0 && !string.IsNullOrEmpty(log.Channel) && HiddenChannels.Contains(log.Channel)) return false;

            if (string.IsNullOrEmpty(Search)) return true;

            return Contains(log.Message, Search) || Contains(log.Channel, Search);
        }

        private static bool Contains(string text, string search) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
