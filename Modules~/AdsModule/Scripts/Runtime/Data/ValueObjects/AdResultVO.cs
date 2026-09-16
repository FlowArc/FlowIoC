using Modules.AdsModule.Enums;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>
    /// The answer to one show, for whoever asked. Reason is the SDK's error or the module's own
    /// word - "ads removed", "12 s until the next interstitial" - and empty on success.
    /// </summary>
    public readonly struct AdResultVO
    {
        public readonly AdFormat Format;
        public readonly string Placement;
        public readonly AdOutcome Outcome;
        public readonly string Reason;
        public readonly AdRewardVO Reward;

        public AdResultVO(AdFormat format, string placement, AdOutcome outcome, string reason = null, AdRewardVO reward = default)
        {
            Format = format;
            Placement = placement ?? string.Empty;
            Outcome = outcome;
            Reason = reason ?? string.Empty;
            Reward = reward;
        }

        public bool IsRewarded => Outcome == AdOutcome.Rewarded;

        public override string ToString() =>
            Reason.Length == 0 ? $"{Format}/{Placement}: {Outcome}" : $"{Format}/{Placement}: {Outcome} ({Reason})";
    }
}
