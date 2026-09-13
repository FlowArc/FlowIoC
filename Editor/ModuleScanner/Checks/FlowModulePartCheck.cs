#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A module's Flow Console channel is declared in the module, in a part of FlowModule at
    /// Scripts/Generated - and beside it an asmref, which is what compiles that part into FlowIoC
    /// rather than into the module's own assembly.
    ///
    /// The part is the channel: there is no list elsewhere that says a module has one, so what
    /// this watches is the file on disk. It can go without anything else changing - deleted by
    /// hand, lost in a merge, or never written because the generator was interrupted - and the
    /// first anyone hears of it is the module failing to compile against a constant that is no
    /// longer declared anywhere.
    ///
    /// A test module has no channel, so it owes no part.
    /// </summary>
    internal class FlowModulePartCheck : IModuleCheck
    {
        private readonly Func<string, bool> _fileExists;
        private readonly Action _regenerate;

        internal FlowModulePartCheck() : this(File.Exists, FlowModuleGenerator.Generate)
        {
        }

        internal FlowModulePartCheck(Func<string, bool> fileExists, Action regenerate)
        {
            _fileExists = fileExists;
            _regenerate = regenerate;
        }

        public string Id => "module-part";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "FlowModule part (a test module has no channel)");

            bool hasPart = _fileExists(PartPathOf(module));
            bool hasAsmRef = _fileExists(AsmRefPathOf(module));

            if (hasPart && hasAsmRef)
                return FindingEVO.Ok(Id, "FlowModule part");

            if (!hasPart)
            {
                return FindingEVO.Fixable(
                    Id,
                    $"{FlowModuleGenerator.GENERATED_FOLDER}/{FlowModuleGenerator.PART_PREFIX}{module.Name}.cs is missing, "
                    + $"so FlowModule.{module.Name} is not declared anywhere and nothing in this module can log on its own channel.");
            }

            return FindingEVO.Fixable(
                Id,
                $"{FlowModuleGenerator.GENERATED_FOLDER}/{FlowModuleGenerator.ASMREF_NAME} is missing, so FlowModule.{module.Name} "
                + "compiles into the module's own assembly instead of FlowIoC's - which makes it a second, unrelated "
                + "FlowModule rather than a part of the one everything else uses.");
        }

        /// <summary>
        /// The generator writes both files for every module, so the repair is to run it rather
        /// than to write the file here. One writer, one shape.
        /// </summary>
        public void Fix(ModuleTargetEVO module) => _regenerate();

        internal static string PartPathOf(ModuleTargetEVO module) =>
            Path.Combine(module.AbsolutePath, FlowModuleGenerator.GENERATED_FOLDER,
                FlowModuleGenerator.PART_PREFIX + module.Name + ".cs");

        internal static string AsmRefPathOf(ModuleTargetEVO module) =>
            Path.Combine(module.AbsolutePath, FlowModuleGenerator.GENERATED_FOLDER, FlowModuleGenerator.ASMREF_NAME);
    }
}

#endif
