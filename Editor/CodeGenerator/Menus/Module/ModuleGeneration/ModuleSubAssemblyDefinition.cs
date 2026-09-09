#if UNITY_EDITOR
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// One of the assemblies a module carves out of itself: an asmdef written into a folder the
    /// layout names, taking that folder out of the module's own assembly, and named after the
    /// module with a suffix.
    ///
    /// A module has two of them. <see cref="SharedAssemblyDefinition"/> is the data it publishes,
    /// and <see cref="SignalsAssemblyDefinition"/> is the signal holder every other module talks
    /// to it through. They are separate assemblies on purpose: while the holder sat inside Shared,
    /// referencing a module to read one published enum also put its signals in scope, and nothing
    /// but discipline stopped a direct cross-module Dispatch.
    ///
    /// The two differ only in a suffix and a folder type, so the walk that finds the folder, the
    /// asmdef that gets written into it and the lookup that reads the name back off disk all live
    /// here.
    /// </summary>
    internal abstract class ModuleSubAssemblyDefinition
    {
        private readonly AssemblyDefinitionTemplate _template;

        protected ModuleSubAssemblyDefinition(AssemblyDefinitionTemplate template)
        {
            _template = template;
        }

        /// <summary>What the module's assembly name gains to become this one's.</summary>
        protected abstract string AssemblySuffix { get; }

        /// <summary>The layout folder this assembly is written into.</summary>
        protected abstract FolderEVO.FolderType FolderType { get; }

        /// <summary>
        /// Writes the assembly for the module at <paramref name="modulePath"/> and hands back its
        /// name, or null when that module has no such folder - which is an ordinary answer rather
        /// than a failure: Shared is optional, and the test module layout has neither folder.
        ///
        /// <paramref name="references"/> is what this assembly has to see. A Signals assembly is
        /// not dependency free: a public signal may be generic over a published type, so the
        /// holder's assembly references whichever Shared assembly that type lives in.
        /// </summary>
        public string CreateFor(string modulePath, DirectoryStructureConfig config, string moduleAssemblyName,
            params string[] references)
        {
            string folderPath = ResolveFolder(modulePath, config);
            if (string.IsNullOrEmpty(folderPath)) return null;

            string assemblyName = moduleAssemblyName + AssemblySuffix;
            File.WriteAllText(
                Path.Combine(folderPath, assemblyName + ".asmdef"),
                _template.Build(assemblyName, references));

            return assemblyName;
        }

        /// <summary>
        /// The name of the assembly the module at <paramref name="modulePath"/> has in this
        /// folder, or null when it has none. Read off the file rather than derived from the module
        /// name, because the module may have been created before the folder existed, or renamed
        /// since.
        /// </summary>
        public string FindIn(string modulePath, DirectoryStructureConfig config)
        {
            string asmdefPath = FindPathIn(modulePath, config);

            return string.IsNullOrEmpty(asmdefPath) ? null : Path.GetFileNameWithoutExtension(asmdefPath);
        }

        /// <summary>
        /// The same assembly as <see cref="FindIn"/>, as the file rather than the name - for a
        /// caller that has to edit the asmdef rather than name it.
        /// </summary>
        public string FindPathIn(string modulePath, DirectoryStructureConfig config)
        {
            string folderPath = ResolveFolder(modulePath, config);
            if (string.IsNullOrEmpty(folderPath)) return null;

            string[] asmdefFiles = Directory.GetFiles(folderPath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return asmdefFiles.Length == 0 ? null : asmdefFiles[0];
        }

        /// <summary>
        /// The folder if the module actually has it on disk. A layout declaring a folder is not
        /// the same as a module having taken it, and every caller here means the second.
        /// </summary>
        internal string ResolveFolder(string modulePath, DirectoryStructureConfig config)
        {
            if (string.IsNullOrEmpty(modulePath) || config == null) return null;

            string folderPath = config.FindFullFolderPathByID(FolderType, modulePath);

            return string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath) ? null : folderPath;
        }
    }
}
#endif