#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using FlowIoC.Editor.CodeGenerator.Menus.Module;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One .csproj.DotSettings file, compared against a plan or written from it. Everything about
    /// the XML format itself stays in NamespaceUtility, which already owns it; this class only
    /// decides whether the file on disk says what the plan says it should.
    ///
    /// The methods are virtual so a test can stand in for the disk - the file is the one part of
    /// DotSettingsCheck that cannot be described in a fixture.
    /// </summary>
    internal class DotSettingsFile
    {
        private readonly Regex _skipKey = new Regex("NamespaceFoldersToSkip/=([^/\"]+)/@EntryIndexedValue");

        internal virtual bool Matches(string path, IReadOnlyList<string> skipFolders)
        {
            if (!File.Exists(path)) return false;

            string content = File.ReadAllText(path);

            // Every spelling, not the first alone: a file written before the invariant lowercase
            // was added carries the folder and still leaves Rider's inspection on, and the check
            // is what sends the scanner to write it again.
            foreach (string folder in skipFolders)
            {
                foreach (string spelling in NamespaceUtility.EncodeSpellings(folder))
                {
                    if (!content.Contains(spelling)) return false;
                }
            }

            return !HasRootedKey(content);
        }

        /// <summary>
        /// A skip entry keyed by an absolute path - D:Work_005CCNYT_005C... - which Rider never
        /// matches, since it keys a folder relative to the project. Create Module wrote such keys
        /// for a parent resolved in another casing, and a file carrying one beside the right keys
        /// used to pass; it is stale, and the repair writes the file again without it.
        /// </summary>
        private bool HasRootedKey(string content)
        {
            foreach (Match match in _skipKey.Matches(content))
            {
                string folder = match.Groups[1].Value;

                if (folder.Contains(":")
                    || folder.IndexOf("_003A", StringComparison.OrdinalIgnoreCase) >= 0
                    || folder.StartsWith("_005C", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        internal virtual void Write(string path, IReadOnlyList<string> skipFolders)
        {
            NamespaceUtility.CreateDotSettingsFile(path);

            var doc = new XmlDocument();
            doc.Load(path);

            foreach (string folder in skipFolders)
                NamespaceUtility.AddNamespaceFolderToSkip(doc, folder);

            NamespaceUtility.SaveDotSettings(doc, path);
        }
    }
}

#endif