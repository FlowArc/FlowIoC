#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// A ReSharper settings file as a set of keyed entries: read off disk, and composed back in
    /// the shape Rider writes - one element a line, sorted by key - so a rewrite stays a small
    /// diff. Both writers FlowIoC keeps, the solution's code style and the package's own
    /// namespace folders, go through here so that they never disagree on the format.
    /// </summary>
    internal class DotSettingsFile
    {
        internal const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        private const string SystemNamespace = "clr-namespace:System;assembly=mscorlib";
        private const string SettingsStorageNamespace = "urn:shemas-jetbrains-com:settings-storage-xaml";
        private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        internal Dictionary<string, SettingsEntry> Read(string filePath)
        {
            var entries = new Dictionary<string, SettingsEntry>(StringComparer.Ordinal);

            var document = new XmlDocument();
            document.Load(filePath);

            if (document.DocumentElement == null)
                return entries;

            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (!(node is XmlElement element))
                    continue;

                string key = element.GetAttribute("Key", XamlNamespace);
                if (string.IsNullOrEmpty(key))
                    continue;

                entries[key] = new SettingsEntry(element.LocalName, element.InnerText);
            }

            return entries;
        }

        internal string Compose(IDictionary<string, SettingsEntry> entries)
        {
            var builder = new StringBuilder();

            builder.Append("<wpf:ResourceDictionary xml:space=\"preserve\"");
            builder.Append(" xmlns:x=\"").Append(XamlNamespace).Append("\"");
            builder.Append(" xmlns:s=\"").Append(SystemNamespace).Append("\"");
            builder.Append(" xmlns:ss=\"").Append(SettingsStorageNamespace).Append("\"");
            builder.Append(" xmlns:wpf=\"").Append(PresentationNamespace).Append("\"");
            builder.Append(">\n");

            // ReSharper keeps the file sorted by key, so a rewrite stays a small diff.
            var sorted = new SortedDictionary<string, SettingsEntry>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, SettingsEntry> entry in entries)
            {
                sorted[entry.Key] = entry.Value;
            }

            foreach (KeyValuePair<string, SettingsEntry> entry in sorted)
            {
                builder.Append("\t<s:").Append(entry.Value.ElementName);
                builder.Append(" x:Key=\"").Append(entry.Key).Append("\">");
                builder.Append(Escape(entry.Value.Value));
                builder.Append("</s:").Append(entry.Value.ElementName).Append(">\n");
            }

            builder.Append("</wpf:ResourceDictionary>\n");

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}

#endif
