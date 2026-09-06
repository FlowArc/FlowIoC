#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The modules nested inside one module, found by walking its folder rather than by asking
    /// the index. The scanner's own targets come from the tree for the same reason: the index is
    /// a cache, and a card that described the cache rather than the disk would be wrong exactly
    /// when the cache has drifted.
    ///
    /// Only the module's own children are found, not everything beneath it. A test module under
    /// a screen module belongs to that screen module and is named on its card; naming it on the
    /// grandparent's card as well would say the module owns something it does not.
    /// </summary>
    internal class ModuleChildren
    {
        private const string MODULE_SUFFIX = "Module";
        private const string SCREEN_FOLDER = "zScreenModules";
        private const string SUB_FOLDER = "zSubModules";
        private const string TEST_FOLDER = "zTestModules";

        internal IReadOnlyList<ScannedModule> Of(string moduleAbsolutePath)
        {
            var found = new List<ScannedModule>();

            if (string.IsNullOrEmpty(moduleAbsolutePath) || !Directory.Exists(moduleAbsolutePath))
                return found;

            foreach (string directory in Directory.GetDirectories(
                         moduleAbsolutePath, "*" + MODULE_SUFFIX, SearchOption.AllDirectories))
            {
                string holder = Path.GetDirectoryName(directory) ?? string.Empty;

                // The holder folder has to sit directly in this module, or the module found is a
                // grandchild that belongs to whichever module does hold it.
                if (!Same(Path.GetDirectoryName(holder), moduleAbsolutePath)) continue;

                ModuleKind kind;

                switch (Path.GetFileName(holder))
                {
                    case SCREEN_FOLDER:
                        kind = ModuleKind.Screen;
                        break;
                    case SUB_FOLDER:
                        kind = ModuleKind.Sub;
                        break;
                    case TEST_FOLDER:
                        kind = ModuleKind.Test;
                        break;
                    default:
                        continue;
                }

                found.Add(new ScannedModule
                {
                    Name = Path.GetFileName(directory),
                    Kind = kind,
                    AbsolutePath = directory,
                });
            }

            return found;
        }

        /// <summary>
        /// Two paths naming the same folder. Windows is case insensitive and the separators a
        /// caller hands in are not always the ones GetDirectories returns.
        /// </summary>
        private bool Same(string left, string right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
    }
}

#endif