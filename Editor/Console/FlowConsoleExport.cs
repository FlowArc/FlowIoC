#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Logs as plain text, for pasting into a bug report or handing to somebody who was not at
    /// the machine. The console's own rich-text colouring is stripped, because a text file should
    /// carry the sentence rather than the markup.
    /// </summary>
    public class FlowConsoleExport
    {
        private static readonly Regex RichText = new("<.*?>", RegexOptions.Compiled);

        public string ToText(IReadOnlyList<ConsoleLog> logs, bool includeStackTrace)
        {
            if (logs == null || logs.Count == 0) return string.Empty;

            var text = new StringBuilder(logs.Count * 128);

            for (int i = 0; i < logs.Count; i++)
            {
                ConsoleLog log = logs[i];

                text.Append(log.Hour.ToString("00")).Append(':')
                    .Append(log.Minute.ToString("00")).Append(':')
                    .Append(log.Second.ToString("00")).Append('.')
                    .Append(log.Millisecond.ToString("000"))
                    .Append(" [").Append(log.SystemLogType).Append(']')
                    .Append(" [").Append(log.LogType).Append("] ")
                    .Append(Strip(log.Message))
                    .Append('\n');

                if (!includeStackTrace || string.IsNullOrEmpty(log.StackTrace)) continue;

                foreach (string line in log.StackTrace.Split('\n'))
                {
                    if (line.Length == 0) continue;
                    text.Append("    ").Append(line.TrimEnd('\r')).Append('\n');
                }
            }

            return text.ToString();
        }

        private static string Strip(string message)
        {
            return message == null ? string.Empty : RichText.Replace(message, string.Empty);
        }
    }
}
#endif
