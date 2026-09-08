#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A module's Flow Console channel is declared in the module, in a part of FlowLogType at
    /// Scripts/Generated - and beside it an asmref, which is what compiles that part into FlowIoC
    /// rather than into the module's own assembly.
    ///
    /// LogTypeCheck already answers whether the channel is registered in CD_FlowConsole. This
    /// answers the other half, which nothing was watching: the file on disk. It can go without the
    /// settings changing at all - deleted by hand, lost in a merge, or never written because the
    /// generator was interrupted - and the first anyone hears of it is the module failing to
    /// compile against a constant that is no longer declared anywhere.
    ///
    /// A test module has no channel, so it owes no part.
    /// </summary>
    internal class LogTypePartCheck : IModuleCheck
    {
        internal const string GENERATED_FOLDER = "Scripts/Generated";
        internal const string ASMREF_NAME = "FlowIoC.Generated.asmref";

        private readonly Func<string, bool> _channelExists;
        private readonly Func<string, bool> _fileExists;
        private readonly Action _regenerate;

        internal LogTypePartCheck() : this(
            channel => FlowLogger.Settings != null && FlowLogger.Settings.TryGetLogType(channel, out _),
            File.Exists,
            FlowLogTypeGenerator.Generate)
        {
        }

        internal LogTypePartCheck(Func<string, bool> channelExists, Func<string, bool> fileExists, Action regenerate)
        {
            _channelExists = channelExists;
            _fileExists = fileExists;
            _regenerate = regenerate;
        }

        public string Id => "log-type-part";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Log type part (a test module has no channel)");

            if (!_channelExists(module.Name))
                return FindingEVO.Ok(Id, "Log type part (module has no channel)");

            bool hasPart = _fileExists(PartPathOf(module));
            bool hasAsmRef = _fileExists(AsmRefPathOf(module));

            if (hasPart && hasAsmRef)
                return FindingEVO.Ok(Id, "Log type part");

            if (!hasPart)
            {
                return FindingEVO.Fixable(
                    Id,
                    $"{GENERATED_FOLDER}/FlowLogType.{module.Name}.cs is missing, so FlowLogType.{module.Name} "
                    + "is not declared anywhere and nothing in this module can log on its own channel.");
            }

            return FindingEVO.Fixable(
                Id,
                $"{GENERATED_FOLDER}/{ASMREF_NAME} is missing, so FlowLogType.{module.Name} compiles into "
                + "the module's own assembly instead of FlowIoC's - which makes it a second, unrelated "
                + "FlowLogType rather than a part of the one everything else uses.");
        }

        /// <summary>
        /// The generator writes both files for every registered channel, so the repair is to run it
        /// rather than to write the file here. One writer, one shape.
        /// </summary>
        public void Fix(ModuleTargetEVO module) => _regenerate();

        internal static string PartPathOf(ModuleTargetEVO module) =>
            Path.Combine(module.AbsolutePath, GENERATED_FOLDER, "FlowLogType." + module.Name + ".cs");

        internal static string AsmRefPathOf(ModuleTargetEVO module) =>
            Path.Combine(module.AbsolutePath, GENERATED_FOLDER, ASMREF_NAME);
    }
}

#endif
