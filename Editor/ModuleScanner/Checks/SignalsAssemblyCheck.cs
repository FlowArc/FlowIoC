#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A module that has a Scripts/Signals folder must have the assembly that folder is for.
    /// Without it the folder still belongs to the module's own assembly, so nothing outside the
    /// module can reach the signal holder at all - and a Connector, which is the one thing that
    /// should, would have to reference the module's Models and Commands to get at it.
    ///
    /// The assembly is written with the module's Shared assembly as a reference, because a public
    /// signal is often generic over a type the module publishes. A holder generic over another
    /// module's published type needs that reference added by hand; this check never rewrites a
    /// reference list, only creates a file that is missing, so a hand-added one survives.
    ///
    /// Test modules are skipped: their holder stays in Scripts/Runtime/Signals, they are allowed
    /// to reference anything directly, and nothing wires to them from outside.
    /// </summary>
    internal class SignalsAssemblyCheck : IModuleCheck
    {
        private readonly Func<ModuleTargetEVO, string> _signalsFolderOf;
        private readonly Func<string, string[]> _asmdefsIn;
        private readonly Func<string, string[]> _scriptsIn;
        private readonly Action<ModuleTargetEVO> _create;

        internal SignalsAssemblyCheck() : this(
            DefaultSignalsFolderOf,
            folder => FilesIn(folder, "*.asmdef"),
            folder => FilesIn(folder, "*.cs"),
            module => new SignalsAssemblyDefinition()
                .CreateFor(module.AbsolutePath, module.Layout, module.ExpectedAssemblyName,
                    new SharedAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout)))
        {
        }

        internal SignalsAssemblyCheck(
            Func<ModuleTargetEVO, string> signalsFolderOf,
            Func<string, string[]> asmdefsIn,
            Action<ModuleTargetEVO> create)
            : this(signalsFolderOf, asmdefsIn, _ => new[] {"Signals.cs"}, create)
        {
        }

        internal SignalsAssemblyCheck(
            Func<ModuleTargetEVO, string> signalsFolderOf,
            Func<string, string[]> asmdefsIn,
            Func<string, string[]> scriptsIn,
            Action<ModuleTargetEVO> create)
        {
            _signalsFolderOf = signalsFolderOf;
            _asmdefsIn = asmdefsIn;
            _scriptsIn = scriptsIn;
            _create = create;
        }

        public string Id => "signals-assembly";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Signals assembly (a test module is wired to directly)");

            string signalsFolder = _signalsFolderOf(module);

            if (string.IsNullOrEmpty(signalsFolder))
                return FindingEVO.Ok(Id, "Signals assembly (module has no Signals folder)");

            if (_asmdefsIn(signalsFolder).Length > 0)
                return FindingEVO.Ok(Id, "Signals assembly");

            // The folder is mandatory, so every module has it whether or not it says anything. A
            // Connector is the honest case: it wires other modules and nothing dispatches into it,
            // so it has no holder - and writing an assembly for the empty folder would ship a DLL
            // with nothing in it, which is the waste that moving Signals out of Shared removed.
            if (_scriptsIn(signalsFolder).Length == 0)
                return FindingEVO.Ok(Id, "Signals assembly (module announces nothing)");

            return FindingEVO.Fixable(
                Id,
                "Scripts/Signals has no assembly, so the module's public signal holder compiles into "
                + "the module's own and nothing may reach it. It should be "
                + $"{module.ExpectedAssemblyName}{SignalsAssemblyDefinition.ASSEMBLY_SUFFIX}.");
        }

        public void Fix(ModuleTargetEVO module) => _create(module);

        /// <summary>
        /// The Signals folder if the module has one on disk, matching what
        /// SignalsAssemblyDefinition itself resolves - a layout that declares the folder is not
        /// the same as a module that took it.
        /// </summary>
        private static string[] FilesIn(string folder, string pattern) =>
            Directory.Exists(folder) ? Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly) : new string[0];

        private static string DefaultSignalsFolderOf(ModuleTargetEVO module)
        {
            if (module?.Layout == null || string.IsNullOrEmpty(module.AbsolutePath)) return null;

            string path = module.Layout.FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, module.AbsolutePath);

            return string.IsNullOrEmpty(path) || !Directory.Exists(path) ? null : path;
        }
    }
}

#endif