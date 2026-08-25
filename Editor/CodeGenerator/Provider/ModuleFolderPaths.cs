#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Provider
{
    /// <summary>A module the index names that no run can do anything with, and why.</summary>
    internal readonly struct SkippedModule
    {
        internal string Name { get; }
        internal string Reason { get; }

        internal SkippedModule(string name, string reason)
        {
            Name = name;
            Reason = reason;
        }
    }

    /// <summary>The folders a run can work with, and the modules it had to leave out.</summary>
    internal readonly struct ModuleFolders
    {
        internal IReadOnlyList<string> Paths { get; }
        internal IReadOnlyList<SkippedModule> Skipped { get; }

        internal ModuleFolders(IReadOnlyList<string> paths, IReadOnlyList<SkippedModule> skipped)
        {
            Paths = paths;
            Skipped = skipped;
        }
    }

    /// <summary>
    /// Turns the modules the index knows about into folders that are really on disk.
    ///
    /// The index is a cache, so it can name a module whose folder has since been deleted, and
    /// there are two different ways that shows up. Sometimes the folder GUID resolves to
    /// nothing; sometimes <c>AssetDatabase.GUIDToAssetPath</c> answers with the last path it
    /// knew, which looks like a perfectly good path right up until the first Directory call
    /// throws <c>DirectoryNotFoundException</c>. Guarding only the first case left the second
    /// one able to take a whole run down over one stale entry.
    ///
    /// What was skipped is reported rather than logged, so the caller decides how loud a stale
    /// index entry should be and this stays testable without a project on disk.
    /// </summary>
    internal class ModuleFolderPaths
    {
        private readonly Func<string, string> _toAbsolutePath;
        private readonly Func<string, bool> _folderExists;

        internal ModuleFolderPaths()
            : this(new ModuleAssetPathResolver().ToAbsolutePath, Directory.Exists)
        {
        }

        internal ModuleFolderPaths(Func<string, string> toAbsolutePath, Func<string, bool> folderExists)
        {
            _toAbsolutePath = toAbsolutePath;
            _folderExists = folderExists;
        }

        internal ModuleFolders Resolve(ModuleRegistry registry)
        {
            var paths = new List<string>();
            var skipped = new List<SkippedModule>();

            foreach (ModuleDescriptor module in registry.Modules)
            {
                string absolutePath = _toAbsolutePath(registry.PathOf(module));

                if (string.IsNullOrEmpty(absolutePath))
                {
                    skipped.Add(new SkippedModule(module.Name,
                        "its folder GUID no longer resolves to a path"));
                    continue;
                }

                if (!_folderExists(absolutePath))
                {
                    skipped.Add(new SkippedModule(module.Name,
                        $"'{absolutePath}' is not on disk"));
                    continue;
                }

                paths.Add(absolutePath);
            }

            return new ModuleFolders(paths, skipped);
        }
    }
}

#endif
