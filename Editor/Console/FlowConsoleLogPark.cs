#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Puts the log list somewhere a domain reload cannot reach and takes it back afterwards.
    /// FlowLogger.Logs is a static, and recompiling resets every static in the project - which is
    /// why the console used to empty itself whenever a script was saved, whatever Clear on
    /// Recompile said. Unity's own console survives because its entries live in native code; ours
    /// survives by being written down.
    /// </summary>
    public class FlowConsoleLogPark
    {
        /// <summary>
        /// A run that logged a hundred thousand lines would put a hundred thousand lines of JSON
        /// through SessionState on every save. The reason to keep the list across a compile is to
        /// read what just happened, so the oldest are the ones that go.
        /// </summary>
        public const int MaxParkedLogs = 5000;

        [Serializable]
        private class ParkedLogs
        {
            public List<ConsoleLog> Logs = new();
        }

        public string Write(IReadOnlyList<ConsoleLog> logs)
        {
            var parked = new ParkedLogs();

            if (logs != null)
            {
                int from = Mathf.Max(0, logs.Count - MaxParkedLogs);

                for (int i = from; i < logs.Count; i++)
                    parked.Logs.Add(logs[i]);
            }

            return JsonUtility.ToJson(parked);
        }

        public List<ConsoleLog> Read(string parked)
        {
            if (string.IsNullOrEmpty(parked)) return new List<ConsoleLog>();

            try
            {
                ParkedLogs read = JsonUtility.FromJson<ParkedLogs>(parked);
                return read?.Logs ?? new List<ConsoleLog>();
            }
            catch (Exception)
            {
                // SessionState outlives the code that wrote it, so a string from an older version
                // of the package can turn up here. It reads as nothing rather than throwing on
                // the first frame after a reload.
                return new List<ConsoleLog>();
            }
        }
    }
}
#endif
