#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// A channel's profile as one line of a module's card, and back:
    ///
    /// <code>
    /// Profile: prefix="[Main]" prefix-style=bold prefix-colour=#39FF00 message-style=italic message-colour=#DDDDDD postfix="!" postfix-style=bold,underline postfix-colour=#9E2FDD
    /// </code>
    ///
    /// Every key is optional and unknown ones are skipped, so a card written by hand with one of
    /// them still parses. A text value is quoted, because a prefix usually holds a space or a
    /// bracket; a colour is a hex string and a style is a comma-joined list of bold, italic and
    /// underline. Both spellings of colour are read; the British one is written, to match the
    /// Colour line beside it.
    /// </summary>
    internal class FlowLogProfileLine
    {
        private static readonly Regex Pair = new Regex(
            "(?<key>[A-Za-z][A-Za-z-]*)=(?:\"(?<quoted>(?:[^\"\\\\]|\\\\.)*)\"|(?<bare>\\S+))",
            RegexOptions.Compiled);

        /// <summary>The profile the line describes, or null when it sets nothing.</summary>
        internal FlowLogProfile Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in Pair.Matches(line))
            {
                string key = match.Groups["key"].Value;
                string value = match.Groups["quoted"].Success
                    ? Unescape(match.Groups["quoted"].Value)
                    : match.Groups["bare"].Value;

                values[key] = value;
            }

            string prefix = Text(values, "prefix");
            string postfix = Text(values, "postfix");
            FlowTextStyle messageStyle = Style(values, "message-style");
            Color messageColor = Colour(values, "message-colour", "message-color");

            bool empty = string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(postfix)
                                                       && messageStyle == FlowTextStyle.None
                                                       && messageColor == Color.white;
            if (empty) return null;

            var profile = new FlowLogProfile();

            if (!string.IsNullOrEmpty(prefix))
                profile.SetPrefix(prefix, Style(values, "prefix-style"), Colour(values, "prefix-colour", "prefix-color"));

            if (messageStyle != FlowTextStyle.None)
                profile.SetMessageStyle(messageStyle);

            if (messageColor != Color.white)
                profile.SetMessageColor(messageColor);

            if (!string.IsNullOrEmpty(postfix))
                profile.SetPostfix(postfix, Style(values, "postfix-style"), Colour(values, "postfix-colour", "postfix-color"));

            return profile;
        }

        /// <summary>The line for a profile, or null for one that decorates nothing.</summary>
        internal string Format(FlowLogProfile profile)
        {
            if (profile == null) return null;

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(profile.Prefix))
            {
                parts.Add("prefix=" + Quote(profile.Prefix));
                AppendStyle(parts, "prefix-style", profile.PrefixStyle);
                AppendColour(parts, "prefix-colour", profile.PrefixColor);
            }

            AppendStyle(parts, "message-style", profile.MessageStyle);
            AppendColour(parts, "message-colour", profile.MessageColor);

            if (!string.IsNullOrEmpty(profile.Postfix))
            {
                parts.Add("postfix=" + Quote(profile.Postfix));
                AppendStyle(parts, "postfix-style", profile.PostfixStyle);
                AppendColour(parts, "postfix-colour", profile.PostfixColor);
            }

            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        private static string Text(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out string value) ? value : null;
        }

        private static FlowTextStyle Style(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
                return FlowTextStyle.None;

            var style = FlowTextStyle.None;

            foreach (string name in value.Split(','))
            {
                if (Enum.TryParse(name.Trim(), true, out FlowTextStyle parsed))
                    style |= parsed;
            }

            return style;
        }

        private static Color Colour(Dictionary<string, string> values, string key, string alternative)
        {
            if (!values.TryGetValue(key, out string value) && !values.TryGetValue(alternative, out value))
                return Color.white;

            string hex = value.StartsWith("#", StringComparison.Ordinal) ? value : "#" + value;

            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private static void AppendStyle(List<string> parts, string key, FlowTextStyle style)
        {
            if (style == FlowTextStyle.None) return;

            var names = new List<string>();
            if ((style & FlowTextStyle.Bold) != 0) names.Add("bold");
            if ((style & FlowTextStyle.Italic) != 0) names.Add("italic");
            if ((style & FlowTextStyle.Underline) != 0) names.Add("underline");

            if (names.Count > 0)
                parts.Add(key + "=" + string.Join(",", names));
        }

        private static void AppendColour(List<string> parts, string key, Color color)
        {
            if (color == Color.white) return;

            parts.Add(key + "=#" + HexOf(color));
        }

        /// <summary>Six digits unless the alpha says otherwise, the way a colour is written by hand.</summary>
        internal static string HexOf(Color color)
        {
            Color32 bytes = color;

            return bytes.a == 255
                ? ColorUtility.ToHtmlStringRGB(color)
                : ColorUtility.ToHtmlStringRGBA(color);
        }

        private static string Quote(string text)
        {
            var builder = new StringBuilder("\"");

            foreach (char character in text)
            {
                if (character == '"' || character == '\\') builder.Append('\\');
                builder.Append(character);
            }

            return builder.Append('"').ToString();
        }

        private static string Unescape(string text)
        {
            var builder = new StringBuilder(text.Length);

            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '\\' && index + 1 < text.Length)
                {
                    builder.Append(text[index + 1]);
                    index++;
                    continue;
                }

                builder.Append(text[index]);
            }

            return builder.ToString();
        }
    }
}
#endif
