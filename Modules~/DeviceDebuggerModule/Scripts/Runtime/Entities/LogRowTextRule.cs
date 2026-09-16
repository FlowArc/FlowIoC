using System.Text.RegularExpressions;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Entities
{
    /// <summary>
    /// What a two-line row says, the way the Flow Console lays one out: the message alone on the
    /// first line, and under it the channel tag in its colour with where the line came from. The
    /// logger writes the tag into the front of the message as rich text; here it is taken off
    /// the message and kept for the second line, so search and copy still see it and the first
    /// line is the message and nothing else.
    /// </summary>
    public class LogRowTextRule
    {
        private readonly Regex _leadingTag = new("^\\s*<color=(#[0-9A-Fa-f]{6,8})>(\\[[^\\]]*\\])</color>\\s*", RegexOptions.Compiled);
        private readonly Regex _richText = new("<[^>]+>", RegexOptions.Compiled);

        public LogRowTextVO Split(ConsoleLog log)
        {
            var row = new LogRowTextVO();

            if (log == null) return row;

            string message = log.Message ?? "";
            Match tag = _leadingTag.Match(message);

            if (tag.Success)
            {
                row.Tag = tag.Groups[2].Value;
                row.TagColor = ColorUtility.TryParseHtmlString(tag.Groups[1].Value, out Color parsed) ? parsed : log.LogColor;
                message = message.Substring(tag.Length);
            }
            else
            {
                row.Tag = string.IsNullOrEmpty(log.Channel) ? "" : "[" + log.Channel + "]";
                row.TagColor = log.LogColor.a > 0f ? log.LogColor : Color.white;
            }

            row.Message = FirstLine(message);
            row.PlainMessage = _richText.Replace(message, "");
            row.Source = SourceOf(log);

            return row;
        }

        /// <summary>Where the line came from: file and line when the build carries them, else the class, else the kind.</summary>
        private static string SourceOf(ConsoleLog log)
        {
            if (!string.IsNullOrEmpty(log.SourceFilePath))
                return FileNameOf(log.SourceFilePath) + ":" + log.SourceLineNumber;

            if (!string.IsNullOrEmpty(log.BlameTypeName)) return log.BlameTypeName;

            if (!string.IsNullOrEmpty(log.SourceClassName)) return log.SourceClassName;

            return log.SystemLogType.ToString();
        }

        /// <summary>The last segment after either separator: a Windows path recorded at compile time is read on a Linux phone.</summary>
        private static string FileNameOf(string path)
        {
            int cut = path.LastIndexOfAny(new[] {'/', '\\'});

            return cut < 0 ? path : path.Substring(cut + 1);
        }

        private static string FirstLine(string message)
        {
            int newline = message.IndexOf('\n');

            return newline < 0 ? message : message.Substring(0, newline);
        }
    }
}