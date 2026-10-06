#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The asmdefs of the project that reference an assembly an update removes, named before
    /// anything is written. Such a reference is the game's own wiring - a Connector that used a
    /// sub module's signals - and what it unlocked is code nobody but the game can rewrite, so the
    /// update does not touch it; it says where it is, so the compile error that follows is not the
    /// first the reader hears of it.
    ///
    /// An assembly is removed when the plan deletes its asmdef, or offers to on a conflict, and the
    /// shipped copy declares that name nowhere - an asmdef the package moved is still there.
    /// </summary>
    internal class RemovedAssemblyReferences
    {
        private const string ASMDEF = ".asmdef";
        private const string GUID_PREFIX = "GUID:";

        /// <summary>One line per reference: the referencing asmdef, project relative, and the assembly.</summary>
        internal IReadOnlyList<string> Find(ModuleUpdatePlanEVO plan, string installedFolder, string shippedFolder,
            string projectRoot)
        {
            var found = new List<string>();
            var going = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> removed = Removed(plan, installedFolder, shippedFolder, going);

            if (removed.Count == 0)
                return found;

            var longPath = new LongPath();
            string assets = longPath.Of(Path.Combine(projectRoot, "Assets"));

            if (!Directory.Exists(assets))
                return found;

            foreach (string asmdef in Directory.GetFiles(assets, "*" + ASMDEF, SearchOption.AllDirectories))
            {
                string plain = Path.GetFullPath(longPath.Strip(asmdef));

                if (going.Contains(plain))
                    continue;

                AssemblyDefinition declaration = Read(asmdef);

                if (declaration?.references == null)
                    continue;

                foreach (string reference in declaration.references)
                {
                    if (reference == null)
                        continue;

                    foreach (KeyValuePair<string, string> assembly in removed)
                    {
                        bool byName = string.Equals(reference, assembly.Key, StringComparison.Ordinal);
                        bool byGuid = assembly.Value != null
                                      && string.Equals(reference, GUID_PREFIX + assembly.Value, StringComparison.OrdinalIgnoreCase);

                        if (byName || byGuid)
                            found.Add($"{Relative(projectRoot, plain)} → {assembly.Key}");
                    }
                }
            }

            found.Sort(StringComparer.Ordinal);

            return found;
        }

        /// <summary>Removed assembly names, each with the GUID its asmdef's meta gives it, or null.</summary>
        private static Dictionary<string, string> Removed(ModuleUpdatePlanEVO plan, string installedFolder,
            string shippedFolder, HashSet<string> going)
        {
            var removed = new Dictionary<string, string>(StringComparer.Ordinal);
            var shippedNames = new HashSet<string>(StringComparer.Ordinal);
            var longPath = new LongPath();

            string shipped = longPath.Of(shippedFolder);

            if (Directory.Exists(shipped))
            {
                foreach (string asmdef in Directory.GetFiles(shipped, "*" + ASMDEF, SearchOption.AllDirectories))
                {
                    string name = Read(asmdef)?.name;

                    if (!string.IsNullOrEmpty(name))
                        shippedNames.Add(name);
                }
            }

            foreach (KeyValuePair<string, ModuleUpdateVerdict> pair in plan.Verdicts)
            {
                if (!pair.Key.EndsWith(ASMDEF, StringComparison.Ordinal))
                    continue;

                bool deletes = pair.Value == ModuleUpdateVerdict.Delete
                               || pair.Value == ModuleUpdateVerdict.RemoveEdited
                               || (pair.Value == ModuleUpdateVerdict.Conflict
                                   && !File.Exists(longPath.Of(Full(shippedFolder, pair.Key))));

                if (!deletes)
                    continue;

                string full = Full(installedFolder, pair.Key);
                string name = Read(longPath.Of(full))?.name;

                going.Add(Path.GetFullPath(full));

                if (string.IsNullOrEmpty(name) || shippedNames.Contains(name))
                    continue;

                removed[name] = GuidOf(longPath.Of(full + ".meta"));
            }

            return removed;
        }

        private static AssemblyDefinition Read(string path)
        {
            try
            {
                return File.Exists(path) ? JsonUtility.FromJson<AssemblyDefinition>(File.ReadAllText(path)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GuidOf(string metaPath)
        {
            if (!File.Exists(metaPath))
                return null;

            foreach (string line in File.ReadAllLines(metaPath))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("guid:", StringComparison.Ordinal))
                    return trimmed.Substring("guid:".Length).Trim();
            }

            return null;
        }

        private static string Full(string root, string relative) =>
            Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        private static string Relative(string projectRoot, string path)
        {
            string root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length) : path)
                .Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// Just the two fields read here. Lower case because that is what the file says and
        /// JsonUtility matches on the field name.
        /// </summary>
        [Serializable]
        private class AssemblyDefinition
        {
            public string name;
            public string[] references;
        }
    }
}

#endif
