#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The module a folder is, and every module nested inside it, by name.
    ///
    /// Delete Module needs this wherever a clean-up pass is keyed on a module's name rather than on
    /// its assemblies. Two were: the screen's Addressables registration, whose group name comes from
    /// the screen module's name, and the FlowLogType channel, which is named for the module. Both
    /// asked only about the module being deleted, so deleting a parent left the registration and the
    /// channel of every screen module inside it behind - an empty Local_Screen- group with its
    /// schema assets, and a column in the Filters panel with nothing to fill it.
    ///
    /// The folder is asked rather than the module index, for the same reason ModuleAssemblies asks
    /// the asmdef: the answer is on disk. This runs on the way to a deletion, before the index is
    /// rebuilt and in a project that may not compile, and an index one rescan out of date would drop
    /// a module silently - which is the bug, back again and harder to see.
    ///
    /// A module is a folder whose name ends in Module, which is what the project says everywhere
    /// else it has to decide this. The container folders are safe from it by their own names:
    /// zScreenModules, zSubModules and zTestModules end in Modules, not Module.
    /// </summary>
    internal class ModuleNames
    {
        private const string MODULE_SUFFIX = "Module";

        private readonly Func<string, IEnumerable<string>> _directoriesUnder;

        internal ModuleNames() : this(DirectoriesUnder)
        {
        }

        internal ModuleNames(Func<string, IEnumerable<string>> directoriesUnder)
        {
            _directoriesUnder = directoriesUnder;
        }

        /// <summary>
        /// <paramref name="modulePath"/> is the module folder on disk and <paramref name="moduleName"/>
        /// the folder's own name. The module itself comes first, then whatever is nested inside it,
        /// in the order it was found.
        /// </summary>
        internal IReadOnlyList<string> Of(string modulePath, string moduleName)
        {
            var names = new List<string>();

            if (IsModule(moduleName)) names.Add(moduleName);

            foreach (string directory in Directories(modulePath))
            {
                string name = NameOf(directory);

                if (!IsModule(name) || names.Contains(name)) continue;

                names.Add(name);
            }

            return names;
        }

        /// <summary>
        /// A folder that cannot be walked is worth no names rather than an exception. This runs on
        /// the way to a deletion, and one unreadable folder is not a reason to leave the rest of the
        /// module's registrations behind.
        /// </summary>
        private IEnumerable<string> Directories(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath)) return Array.Empty<string>();

            try
            {
                return _directoriesUnder(modulePath) ?? Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// The suffix has to be more than the whole name: a folder called Module names no module,
        /// and stripping the suffix off it would leave nothing to look a group or a channel up by.
        /// </summary>
        private static bool IsModule(string name) =>
            !string.IsNullOrEmpty(name)
            && name.Length > MODULE_SUFFIX.Length
            && name.EndsWith(MODULE_SUFFIX, StringComparison.Ordinal);

        private static string NameOf(string directory) =>
            Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        private static IEnumerable<string> DirectoriesUnder(string modulePath) =>
            Directory.Exists(modulePath)
                ? Directory.EnumerateDirectories(modulePath, "*", SearchOption.AllDirectories)
                : Array.Empty<string>();
    }
}

#endif
