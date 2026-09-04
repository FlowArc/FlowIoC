#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A module that has a Scripts/Shared folder must have the assembly that folder is for.
    /// Without it the folder still belongs to the module's own assembly, so the data the module
    /// means to publish is reachable only by code that may also reach its Models and Commands -
    /// the one thing the architecture does not allow.
    ///
    /// Test modules are skipped: they may reference anything directly and publish nothing.
    /// </summary>
    internal class SharedAssemblyCheck : IModuleCheck
    {
        private readonly Func<ModuleTargetEVO, string> _sharedFolderOf;
        private readonly Func<string, string[]> _asmdefsIn;
        private readonly Action<ModuleTargetEVO> _create;

        internal SharedAssemblyCheck() : this(
            DefaultSharedFolderOf,
            folder => Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.asmdef", SearchOption.TopDirectoryOnly)
                : new string[0],
            module => new SharedAssemblyDefinition()
                .CreateFor(module.AbsolutePath, module.Layout, module.ExpectedAssemblyName))
        {
        }

        internal SharedAssemblyCheck(
            Func<ModuleTargetEVO, string> sharedFolderOf,
            Func<string, string[]> asmdefsIn,
            Action<ModuleTargetEVO> create)
        {
            _sharedFolderOf = sharedFolderOf;
            _asmdefsIn = asmdefsIn;
            _create = create;
        }

        public string Id => "shared-assembly";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Shared assembly (test modules publish nothing)");

            string sharedFolder = _sharedFolderOf(module);

            if (string.IsNullOrEmpty(sharedFolder))
                return FindingEVO.Ok(Id, "Shared assembly (module publishes nothing)");

            if (_asmdefsIn(sharedFolder).Length > 0)
                return FindingEVO.Ok(Id, "Shared assembly");

            return FindingEVO.Fixable(
                Id,
                "Scripts/Shared has no assembly, so the data it holds compiles into the module's own. "
                + $"It should be {module.ExpectedAssemblyName}{SharedAssemblyDefinition.ASSEMBLY_SUFFIX}.");
        }

        public void Fix(ModuleTargetEVO module) => _create(module);

        /// <summary>
        /// The Shared folder if the module has one on disk, matching what
        /// SharedAssemblyDefinition itself resolves - a layout that declares the folder is not
        /// the same as a module that took it.
        /// </summary>
        private static string DefaultSharedFolderOf(ModuleTargetEVO module)
        {
            if (module?.Layout == null || string.IsNullOrEmpty(module.AbsolutePath)) return null;

            string path = module.Layout.FindFullFolderPathByID(FolderEVO.FolderType.Shared, module.AbsolutePath);

            return string.IsNullOrEmpty(path) || !Directory.Exists(path) ? null : path;
        }
    }
}

#endif
