#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.ModuleInstall;
using FlowIoC.Editor.Modules;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// What the planner reads the project through: the layout settings, the disk and the loaded
    /// domain, each behind a delegate so a test can describe a project in a dictionary. The
    /// parameterless constructor fills in the real ones.
    /// </summary>
    internal class RenameProjectLookups
    {
        /// <summary>The absolute path of one layout folder inside a module, or null when the module does not have it.</summary>
        internal Func<string, ModuleKind, FolderEVO.FolderType, string> FolderOf { get; set; }

        /// <summary>The files directly inside a folder, whatever their extension; nothing for a folder that is not there.</summary>
        internal Func<string, IEnumerable<string>> FilesIn { get; set; }

        internal Func<string, string> ReadText { get; set; }

        internal Func<string, bool> DirectoryExists { get; set; }

        /// <summary>Every assembly an asmdef under Assets or Packages declares, by file name.</summary>
        internal Func<IReadOnlyCollection<string>> AllAssemblyNames { get; set; }

        /// <summary>The loaded types with this simple name, wherever they live.</summary>
        internal Func<string, IReadOnlyList<TypeHomeEVO>> TypesNamed { get; set; }

        /// <summary>
        /// The assemblies of the ready-made modules the package ships, by the asmdef at the top of
        /// each payload folder. The installer recognises an installed module by that name.
        /// </summary>
        internal Func<IReadOnlyCollection<string>> ShippedAssemblyNames { get; set; }

        internal RenameProjectLookups()
        {
            var configs = new DirectoryStructureConfigProvider();
            var types = new LoadedTypeNames();

            FolderOf = (modulePath, kind, type) =>
            {
                DirectoryStructureConfig config = configs.ConfigFor(kind);
                string folder = config == null ? null : config.FindFullFolderPathByID(type, modulePath);

                return string.IsNullOrEmpty(folder) || !Directory.Exists(folder) ? null : folder;
            };

            FilesIn = folder => Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();

            ReadText = File.ReadAllText;
            DirectoryExists = Directory.Exists;
            AllAssemblyNames = AssemblyNamesOnDisk;
            TypesNamed = types.Named;
            ShippedAssemblyNames = ShippedAssemblyNamesOnDisk;
        }

        /// <summary>
        /// The top asmdef of every payload folder under the package's Modules~, by file name -
        /// which is the assembly name, as ModuleInstaller reads it. A package whose payloads cannot
        /// be listed ships nothing this can name.
        /// </summary>
        private static IReadOnlyCollection<string> ShippedAssemblyNamesOnDisk()
        {
            var names = new List<string>();

            if (!new ModulesSource().TryList(out string[] folders, out _)) return names;

            foreach (string folder in folders)
            {
                foreach (string asmdef in Directory.GetFiles(folder, "*.asmdef", SearchOption.TopDirectoryOnly))
                    names.Add(Path.GetFileNameWithoutExtension(asmdef));
            }

            return names;
        }

        /// <summary>The same list ModuleTargetFactory hands the orphan sweep: every asmdef file under Assets and Packages.</summary>
        private static IReadOnlyCollection<string> AssemblyNamesOnDisk()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var names = new List<string>();

            foreach (string searchPath in new[] {Path.Combine(projectRoot, "Assets"), Path.Combine(projectRoot, "Packages")})
            {
                if (!Directory.Exists(searchPath)) continue;

                foreach (string asmdef in Directory.GetFiles(searchPath, "*.asmdef", SearchOption.AllDirectories))
                    names.Add(Path.GetFileNameWithoutExtension(asmdef));
            }

            return names;
        }
    }
}
#endif
