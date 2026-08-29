#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace FlowIoC.Editor.CodeStyle
{
    internal readonly struct SettingsEntry
    {
        internal string ElementName { get; }
        internal string Value { get; }

        internal SettingsEntry(string elementName, string value)
        {
            ElementName = elementName;
            Value = value;
        }
    }

    /// <summary>
    /// Writes the FlowIoC code style into the consumer project's solution level ReSharper
    /// settings. Rider only reads a file named after the solution and that name differs per
    /// project, so the settings are written rather than shipped as a fixed file. Only the keys
    /// FlowIoC ships are touched; whatever else the team put in the file is left alone. The
    /// project root and the template path are injected so the whole thing can be exercised
    /// against a temporary directory in tests.
    /// </summary>
    internal class SolutionDotSettingsWriter
    {
        internal const string SolutionExtension = ".sln";
        internal const string SettingsExtension = ".sln.DotSettings";

        private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private const string SystemNamespace = "clr-namespace:System;assembly=mscorlib";
        private const string SettingsStorageNamespace = "urn:shemas-jetbrains-com:settings-storage-xaml";
        private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        private readonly string _projectRoot;
        private readonly string _templatePath;

        internal SolutionDotSettingsWriter(string projectRoot, string templatePath)
        {
            _projectRoot = projectRoot;
            _templatePath = templatePath;
        }

        internal bool TryWrite(out string path, out string error)
        {
            return TryWrite(out path, out error, out _);
        }

        /// <summary>
        /// <paramref name="changed"/> tells a caller that runs on its own whether anything was
        /// actually written. The file the merge produces is usually the file already on disk, and
        /// a run that says so can stay silent instead of announcing a rewrite of the same bytes.
        /// </summary>
        internal bool TryWrite(out string path, out string error, out bool changed)
        {
            path = null;
            error = null;
            changed = false;

            if (!File.Exists(_templatePath))
            {
                error = $"FlowIoC could not find its code style at '{_templatePath}'. "
                        + $"Expected {PackageCodeStyleTemplate.Folder}/{PackageCodeStyleTemplate.FileName} inside the package.";
                return false;
            }

            try
            {
                path = Path.Combine(_projectRoot, ResolveSolutionName() + SettingsExtension);

                Dictionary<string, SettingsEntry> entries =
                    File.Exists(path) ? ReadEntries(path) : new Dictionary<string, SettingsEntry>(StringComparer.Ordinal);

                foreach (KeyValuePair<string, SettingsEntry> shipped in ReadEntries(_templatePath))
                {
                    entries[shipped.Key] = shipped.Value;
                }

                string content = Compose(entries);

                changed = !File.Exists(path)
                          || !string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal);

                if (changed)
                    File.WriteAllText(path, content);

                return true;
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not write '{path}': {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// Removes settings files left behind by a solution that no longer exists. Only runs
        /// once a solution is present, so a project Unity has not generated yet keeps its file.
        /// </summary>
        internal string[] CleanupOrphaned()
        {
            var removed = new List<string>();

            string[] solutions = FindSolutions();
            if (solutions.Length == 0)
                return removed.ToArray();

            var solutionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string solution in solutions)
            {
                solutionNames.Add(Path.GetFileNameWithoutExtension(solution));
            }

            foreach (string file in Directory.GetFiles(_projectRoot, "*" + SettingsExtension, SearchOption.TopDirectoryOnly))
            {
                if (!file.EndsWith(SettingsExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = Path.GetFileName(file);
                name = name.Substring(0, name.Length - SettingsExtension.Length);

                if (solutionNames.Contains(name))
                    continue;

                File.Delete(file);
                removed.Add(file);
            }

            return removed.ToArray();
        }

        private string ResolveSolutionName()
        {
            string folderName = new DirectoryInfo(_projectRoot).Name;

            string[] solutions = FindSolutions();
            if (solutions.Length == 0)
            {
                // Unity has not generated the solution yet, and it names it after this folder.
                return folderName;
            }

            foreach (string solution in solutions)
            {
                string name = Path.GetFileNameWithoutExtension(solution);
                if (string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            Array.Sort(solutions, StringComparer.Ordinal);
            return Path.GetFileNameWithoutExtension(solutions[0]);
        }

        private string[] FindSolutions()
        {
            if (!Directory.Exists(_projectRoot))
                return Array.Empty<string>();

            var solutions = new List<string>();
            foreach (string file in Directory.GetFiles(_projectRoot, "*" + SolutionExtension, SearchOption.TopDirectoryOnly))
            {
                if (file.EndsWith(SolutionExtension, StringComparison.OrdinalIgnoreCase))
                    solutions.Add(file);
            }

            return solutions.ToArray();
        }

        private Dictionary<string, SettingsEntry> ReadEntries(string filePath)
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

        private string Compose(IDictionary<string, SettingsEntry> entries)
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

        private string Escape(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}

#endif