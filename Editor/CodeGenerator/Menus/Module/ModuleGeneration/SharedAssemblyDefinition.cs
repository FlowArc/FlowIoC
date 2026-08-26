#if UNITY_EDITOR
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The assembly a module's Shared folder becomes, and the lookup that finds it again.
    ///
    /// A module publishes its data through this assembly rather than through the one holding its
    /// Models and Commands, so a screen or sub module can read a config asset the parent authored
    /// without gaining access to the parent's logic. The asmdef sits inside Scripts/Shared, which
    /// is enough on its own to carve that folder out of the module's own assembly - Unity gives
    /// every file to the nearest asmdef above it - so the module has to reference the Shared
    /// assembly to reach its own shared data.
    ///
    /// It also puts the namespace where it belongs: a value object under
    /// Scripts/Shared/Data/ValueObjects lands in Modules.PlayerModule.Shared.Data.ValueObjects
    /// and cannot collide with the Runtime type of the same name in
    /// Modules.PlayerModule.Data.ValueObjects. That needs a .csproj.DotSettings of its own -
    /// see ModuleGenerator.AddSharedNamespaceExceptions - because such a file only applies to
    /// the project it is named after.
    /// </summary>
    internal class SharedAssemblyDefinition
    {
        internal const string ASSEMBLY_SUFFIX = ".Shared";

        private readonly AssemblyDefinitionTemplate _template;

        public SharedAssemblyDefinition() : this(new AssemblyDefinitionTemplate())
        {
        }

        internal SharedAssemblyDefinition(AssemblyDefinitionTemplate template)
        {
            _template = template;
        }

        /// <summary>
        /// Writes the Shared assembly for the module at <paramref name="modulePath"/> and hands
        /// back its name, or null when that module has no Shared folder - which is the ordinary
        /// case: Shared is an optional folder, and the screen and test module layouts do not offer
        /// it at all.
        /// </summary>
        public string CreateFor(string modulePath, DirectoryStructureConfig config, string moduleAssemblyName)
        {
            string sharedFolderPath = ResolveSharedFolder(modulePath, config);
            if (string.IsNullOrEmpty(sharedFolderPath)) return null;

            string sharedAssemblyName = moduleAssemblyName + ASSEMBLY_SUFFIX;
            File.WriteAllText(
                Path.Combine(sharedFolderPath, sharedAssemblyName + ".asmdef"),
                _template.Build(sharedAssemblyName, null));

            return sharedAssemblyName;
        }

        /// <summary>
        /// The name of the Shared assembly the module at <paramref name="modulePath"/> publishes,
        /// or null when it publishes none. Read off the file rather than derived from the module
        /// name, because the module may have been created before Shared existed, or renamed since.
        /// </summary>
        public string FindIn(string modulePath, DirectoryStructureConfig config)
        {
            string sharedFolderPath = ResolveSharedFolder(modulePath, config);
            if (string.IsNullOrEmpty(sharedFolderPath)) return null;

            string[] asmdefFiles = Directory.GetFiles(sharedFolderPath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return asmdefFiles.Length == 0 ? null : Path.GetFileNameWithoutExtension(asmdefFiles[0]);
        }

        private string ResolveSharedFolder(string modulePath, DirectoryStructureConfig config)
        {
            if (string.IsNullOrEmpty(modulePath) || config == null) return null;

            string sharedFolderPath = config.FindFullFolderPathByID(FolderConfig.FolderType.Shared, modulePath);

            return string.IsNullOrEmpty(sharedFolderPath) || !Directory.Exists(sharedFolderPath) ? null : sharedFolderPath;
        }
    }
}
#endif