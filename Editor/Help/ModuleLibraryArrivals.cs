#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The arrivals record in EditorPrefs, per reader, per project and per package - which
    /// modules this reader has already seen is theirs, the way What's New's last-seen version
    /// is. Three fields on one line: the version, the shipped folders, the new ones.
    /// </summary>
    internal class ModuleLibraryArrivals
    {
        private const string KEY_PREFIX = "FlowIoC.ModuleLibrary.Arrivals.";
        private const string CURRENT_PUBLISHER = "com.birrstudio.";
        private const string FORMER_PUBLISHER = "com.flowarc.";
        private const char FIELD = '|';
        private const char ITEM = ',';

        private readonly string _key;
        private readonly string _formerKey;
        private readonly string _version;
        private readonly ModuleLibraryArrivalsRule _rule = new ModuleLibraryArrivalsRule();

        internal ModuleLibraryArrivals(string packageName, string packageVersion, string projectRoot)
        {
            string project = "." + (projectRoot ?? string.Empty).Replace('\\', '/').ToLowerInvariant();

            _key = KEY_PREFIX + packageName + project;
            _formerKey = packageName != null && packageName.StartsWith(CURRENT_PUBLISHER, StringComparison.Ordinal)
                ? KEY_PREFIX + FORMER_PUBLISHER + packageName.Substring(CURRENT_PUBLISHER.Length) + project
                : null;
            _version = packageVersion ?? string.Empty;
        }

        /// <summary>The folders new to this package version, the record brought up to date on the way.</summary>
        internal IReadOnlyList<string> NewFolders(IReadOnlyList<string> shippedFolders)
        {
            bool moved = TakeFormerRecord();
            ModuleLibraryArrivalsEVO previous = Parse(EditorPrefs.GetString(_key, string.Empty));
            ModuleLibraryArrivalsEVO next = _rule.Next(previous, _version, shippedFolders);

            if (moved || !ReferenceEquals(previous, next))
                EditorPrefs.SetString(_key, Serialize(next));

            return next.New;
        }

        /// <summary>
        /// Carries the record kept under the package's name from before FlowArc became Birr Studio
        /// across to the current name, once, so the modules this reader has seen stay seen.
        /// </summary>
        private bool TakeFormerRecord()
        {
            if (_formerKey == null || !EditorPrefs.HasKey(_formerKey))
                return false;

            if (!EditorPrefs.HasKey(_key))
                EditorPrefs.SetString(_key, EditorPrefs.GetString(_formerKey, string.Empty));

            EditorPrefs.DeleteKey(_formerKey);
            return true;
        }

        internal static string Serialize(ModuleLibraryArrivalsEVO record) =>
            record.Version + FIELD + string.Join(ITEM.ToString(), record.Shipped) + FIELD + string.Join(ITEM.ToString(), record.New);

        internal static ModuleLibraryArrivalsEVO Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string[] fields = text.Split(FIELD);

            if (fields.Length != 3)
                return null;

            return new ModuleLibraryArrivalsEVO
            {
                Version = fields[0],
                Shipped = fields[1].Split(new[] {ITEM}, StringSplitOptions.RemoveEmptyEntries),
                New = fields[2].Split(new[] {ITEM}, StringSplitOptions.RemoveEmptyEntries)
            };
        }
    }
}

#endif