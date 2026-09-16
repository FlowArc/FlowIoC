#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FlowIoC.Editor.ModuleCards;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The module's memory of what the package shipped: .flowioc-shipped.json beside the card,
    /// one hash per file. The publisher writes it into the shipped copy, the installer copies it
    /// in with everything else, and the updater compares three things against it - the shipped
    /// record, to know what the package changed; the installed files, to know what the game
    /// changed - and rewrites it afterwards.
    ///
    /// The leading dot keeps Unity from importing it, so it has no meta and no row in the Project
    /// window; git tracks it like any file.
    ///
    /// Three rules decide what a hash is of. The card is hashed with its generated block and its
    /// Version line stripped, because the block is rewritten locally on every compile and the
    /// version line by the tool. Text is hashed with every CR before an LF dropped, because a
    /// checkout with autocrlf would otherwise read as a game that edited every file. And
    /// Scripts/Generated is not tracked at all: the rescan rewrites it, so it is always the
    /// package's and never a conflict.
    /// </summary>
    internal class ShippedRecord
    {
        internal const string FILE_NAME = ".flowioc-shipped.json";

        private const string FILES_KEY = "files";
        private const string CARD = "MODULE.md";
        private const string GENERATED = "Scripts/Generated/";
        private const string BLOCK_BEGIN = "<!-- FLOWIOC:BEGIN";
        private const string BLOCK_END = "<!-- FLOWIOC:END";

        internal ShippedRecordEVO Build(string moduleFolder)
        {
            var record = new ShippedRecordEVO();

            foreach (string relative in FilesUnder(moduleFolder))
                record.Files[relative] = HashOf(moduleFolder, relative);

            return record;
        }

        /// <summary>The record in the folder, or null when it keeps none.</summary>
        internal ShippedRecordEVO Read(string moduleFolder)
        {
            string path = Path.Combine(moduleFolder, FILE_NAME);

            if (!File.Exists(path))
                return null;

            var record = new ShippedRecordEVO();
            var files = JObject.Parse(File.ReadAllText(path))[FILES_KEY] as JObject;

            if (files == null)
                return record;

            foreach (JProperty property in files.Properties())
                record.Files[property.Name] = property.Value.ToString();

            return record;
        }

        internal void Write(string moduleFolder, ShippedRecordEVO record)
        {
            var files = new JObject();

            foreach (KeyValuePair<string, string> pair in record.Files)
                files[pair.Key] = pair.Value;

            var root = new JObject {[FILES_KEY] = files};

            Directory.CreateDirectory(moduleFolder);
            File.WriteAllText(Path.Combine(moduleFolder, FILE_NAME), root.ToString(Formatting.Indented) + "\n");
        }

        internal bool IsGenerated(string relativePath) =>
            relativePath.StartsWith(GENERATED, StringComparison.Ordinal)
            || relativePath.IndexOf("/" + GENERATED, StringComparison.Ordinal) >= 0;

        internal bool IsTracked(string relativePath) =>
            !string.Equals(relativePath, FILE_NAME, StringComparison.Ordinal) && !IsGenerated(relativePath);

        /// <summary>Every tracked file under the folder, relative, forward-slashed, sorted.</summary>
        internal IReadOnlyList<string> FilesUnder(string moduleFolder) =>
            Under(moduleFolder, IsTracked);

        internal IReadOnlyList<string> GeneratedFilesUnder(string moduleFolder) =>
            Under(moduleFolder, IsGenerated);

        /// <summary>The hash of one file under the rules above, or null when it is not there.</summary>
        internal string HashOf(string moduleFolder, string relativePath)
        {
            string path = Path.Combine(moduleFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(path))
                return null;

            byte[] bytes = string.Equals(relativePath, CARD, StringComparison.Ordinal)
                ? Encoding.UTF8.GetBytes(StripCard(File.ReadAllText(path)))
                : File.ReadAllBytes(path);

            using (SHA1 sha = SHA1.Create())
                return Hex(sha.ComputeHash(WithoutCarriageReturns(bytes)));
        }

        private static IReadOnlyList<string> Under(string root, Func<string, bool> keep)
        {
            var found = new List<string>();

            if (!Directory.Exists(root))
                return found;

            int prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;

            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(prefix).Replace(Path.DirectorySeparatorChar, '/');

                if (keep(relative))
                    found.Add(relative);
            }

            found.Sort(StringComparer.Ordinal);

            return found;
        }

        /// <summary>
        /// The card without its generated block, without its Version line, and without blank
        /// lines. The tool that writes the version line places it beside the other tool lines
        /// and may add or drop a blank line doing so, so blank lines cannot count as content.
        /// </summary>
        private static string StripCard(string text)
        {
            var kept = new StringBuilder();
            var inBlock = false;

            foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith(BLOCK_BEGIN, StringComparison.Ordinal))
                    inBlock = true;

                if (!inBlock && line.Trim().Length > 0 && !ModuleCardVersionLine.IsLine(line))
                    kept.Append(line.TrimEnd()).Append('\n');

                if (trimmed.StartsWith(BLOCK_END, StringComparison.Ordinal))
                    inBlock = false;
            }

            return kept.ToString();
        }

        /// <summary>
        /// Bytes with every CR that precedes an LF dropped, unless they hold a NUL, which makes
        /// them a binary that is hashed as it is.
        /// </summary>
        private static byte[] WithoutCarriageReturns(byte[] bytes)
        {
            if (Array.IndexOf(bytes, (byte) 0) >= 0)
                return bytes;

            var output = new List<byte>(bytes.Length);

            for (int index = 0; index < bytes.Length; index++)
            {
                if (bytes[index] == 13 && index + 1 < bytes.Length && bytes[index + 1] == 10)
                    continue;

                output.Add(bytes[index]);
            }

            return output.ToArray();
        }

        private static string Hex(byte[] hash)
        {
            var text = new StringBuilder(hash.Length * 2);

            foreach (byte value in hash)
                text.Append(value.ToString("x2"));

            return text.ToString();
        }
    }
}

#endif
