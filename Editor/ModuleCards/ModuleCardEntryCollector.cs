#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Walks every card root and reads the authored half of each card it finds. Test modules are
    /// skipped: nothing is routed to one, and it is already named on its parent's card.
    ///
    /// A module with no card, or one whose purpose is still the stub's italics, is listed all the
    /// same with an empty purpose. Leaving it out would hide the module from the one file an
    /// agent is told to read first, which is worse than admitting nobody has described it yet.
    /// </summary>
    internal class ModuleCardEntryCollector
    {
        private const string MODULE_SUFFIX = "Module";
        private const string SCREEN_FOLDER = "zScreenModules";
        private const string SUB_FOLDER = "zSubModules";
        private const string TEST_FOLDER = "zTestModules";

        private readonly ModuleCardFile _file = new ModuleCardFile();
        private readonly ModuleCardReader _reader = new ModuleCardReader();

        internal IReadOnlyList<ModuleCardEntryEVO> Collect(string projectRoot)
        {
            var entries = new List<ModuleCardEntryEVO>();

            var scannerRoots = new HashSet<string>(
                new ModuleScannerRoots().All(projectRoot).Select(root => Relative(projectRoot, root)),
                StringComparer.OrdinalIgnoreCase);

            foreach (string root in new ModuleCardRoots().All(projectRoot))
            {
                string group = Relative(projectRoot, root);

                foreach (string folder in ModuleFoldersUnder(root))
                {
                    ModuleKind kind = KindOf(folder);

                    if (kind == ModuleKind.Test) continue;

                    string card = _file.Read(folder);
                    ModuleCardAuthoredEVO authored = _reader.Read(card);

                    entries.Add(new ModuleCardEntryEVO
                    {
                        Name = Path.GetFileName(folder),
                        Kind = kind,
                        Group = group,
                        RelativePath = Relative(projectRoot, folder),
                        Depth = Depth(root, folder),
                        HasCard = card != null,
                        ScannerOwned = scannerRoots.Contains(group),
                        Purpose = authored.PurposeIsPlaceholder ? null : authored.Purpose,
                        Concepts = authored.ConceptsIsPlaceholder ? null : authored.Concepts,
                    });
                }
            }

            entries.Sort(Compare);
            return entries;
        }

        /// <summary>
        /// Grouped, then by path, so a nested module always follows the one it sits in and the
        /// order never depends on how the file system happened to enumerate.
        /// </summary>
        private int Compare(ModuleCardEntryEVO left, ModuleCardEntryEVO right)
        {
            int group = string.Compare(left.Group, right.Group, StringComparison.Ordinal);
            return group != 0 ? group : string.Compare(left.RelativePath, right.RelativePath, StringComparison.Ordinal);
        }

        private IEnumerable<string> ModuleFoldersUnder(string root)
        {
            if (!Directory.Exists(root)) yield break;

            foreach (string folder in Directory.GetDirectories(root, "*" + MODULE_SUFFIX, SearchOption.AllDirectories))
                yield return folder;
        }

        private ModuleKind KindOf(string folder)
        {
            switch (Path.GetFileName(Path.GetDirectoryName(folder) ?? string.Empty))
            {
                case SCREEN_FOLDER: return ModuleKind.Screen;
                case SUB_FOLDER: return ModuleKind.Sub;
                case TEST_FOLDER: return ModuleKind.Test;
                default: return ModuleKind.Main;
            }
        }

        /// <summary>
        /// A nested module sits two segments below the one it belongs to - zScreenModules and
        /// then the module itself - so the segment count halves into a depth.
        /// </summary>
        private int Depth(string root, string folder)
        {
            string relative = folder.Substring(root.Length).Replace('\\', '/').Trim('/');

            return (relative.Split('/').Length - 1) / 2;
        }

        private string Relative(string projectRoot, string path)
        {
            string normalized = path.Replace('\\', '/');
            string root = (projectRoot ?? string.Empty).Replace('\\', '/').TrimEnd('/');

            return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(root.Length).TrimStart('/')
                : normalized;
        }
    }
}

#endif