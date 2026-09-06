#if UNITY_EDITOR

using System;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Whether a module's MODULE.md is there and whether its generated block still describes the
    /// module. The card is what an agent reads to decide the work belongs here, so a card that
    /// was never filled in is reported rather than passed over - but only as Manual, because
    /// saying what a module is for is the one part of it nothing can generate.
    ///
    /// Test modules carry no card. Nothing is routed to a test module: it is named after the
    /// module it drives and it is listed on that module's card already.
    /// </summary>
    internal class ModuleCardCheck : IModuleCheck
    {
        private readonly Func<ModuleTargetEVO, string> _readCard;
        private readonly Action<ModuleTargetEVO, string> _writeCard;
        private readonly Func<ModuleTargetEVO, ModuleFactsEVO> _factsOf;

        private readonly ModuleCardStub _stub = new ModuleCardStub();
        private readonly ModuleCardReader _reader = new ModuleCardReader();
        private readonly ModuleCardWriter _writer = new ModuleCardWriter();
        private readonly ModuleCardBodyBuilder _builder = new ModuleCardBodyBuilder();

        internal ModuleCardCheck() : this(null, null, null)
        {
        }

        internal ModuleCardCheck(
            Func<ModuleTargetEVO, string> readCard,
            Action<ModuleTargetEVO, string> writeCard,
            Func<ModuleTargetEVO, ModuleFactsEVO> factsOf)
        {
            _readCard = readCard ?? (module => new ModuleCardFile().Read(module.AbsolutePath));
            _writeCard = writeCard ?? ((module, text) => new ModuleCardFile().Write(module.AbsolutePath, text));
            _factsOf = factsOf ?? (module => new ModuleFactsCollector()
                .Collect(module, new ModuleChildren().Of(module.AbsolutePath)));
        }

        public string Id => "module-card";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Module card (a test module carries none)");

            string card = _readCard(module);
            ModuleFactsEVO facts = _factsOf(module);

            // The module folder while there is no card to point at, so the row still takes the
            // reader somewhere.
            string asset = card == null ? module.AssetPath : CardAssetPath(module);

            if (card == null)
                return FindingEVO.Fixable(Id, "Module card - " + ModuleCardFile.FILE_NAME + " is missing", asset);

            // A module whose assembly is not loaded cannot be described, and the block it already
            // carries is left exactly as it is. A card claiming a module has no signals because
            // nothing compiled is worse than one that is a day out of date.
            if (facts == null)
                return FindingEVO.Manual(Id,
                    "Module card cannot be refreshed until the project compiles - "
                    + module.ExpectedAssemblyName + " is not loaded", asset);

            string body = _builder.Build(facts);

            BlockWriteResult probe = _writer.Write(card, body);

            if (probe.Status == BlockWriteStatus.Refused)
                return FindingEVO.Manual(Id, "Module card - " + probe.Message, asset);

            if (_writer.IsStale(card, body))
                return FindingEVO.Fixable(Id, "Module card - the generated block is out of date", asset);

            ModuleCardAuthoredEVO authored = _reader.Read(card);

            if (authored.PurposeIsPlaceholder)
                return FindingEVO.Manual(Id, "Module card - purpose not written yet", asset);

            if (authored.ConceptsIsPlaceholder)
                return FindingEVO.Manual(Id, "Module card - concepts not written yet", asset);

            return FindingEVO.Ok(Id, "Module card", asset);
        }

        public void Fix(ModuleTargetEVO module)
        {
            string card = _readCard(module) ?? _stub.For(module.Name);
            ModuleFactsEVO facts = _factsOf(module);

            if (facts == null) return;

            BlockWriteResult result = _writer.Write(card, _builder.Build(facts));

            if (result.Status == BlockWriteStatus.Refused || result.Status == BlockWriteStatus.Unchanged) return;

            _writeCard(module, result.Text);
        }

        /// <summary>
        /// The card as the asset database knows it. ModuleTargetEVO carries the module's path
        /// relative to the project alongside the absolute one, and only the relative one can be
        /// loaded and pinged.
        /// </summary>
        private string CardAssetPath(ModuleTargetEVO module)
        {
            return string.IsNullOrEmpty(module.AssetPath)
                ? null
                : module.AssetPath.Replace('\\', '/').TrimEnd('/') + "/" + ModuleCardFile.FILE_NAME;
        }
    }
}

#endif