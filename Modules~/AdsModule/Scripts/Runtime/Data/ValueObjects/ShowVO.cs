using System;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>
    /// The show in progress: what was asked, who asked, and what the provider has said so far.
    /// The token tells a timeout started for this show from one started for a later one.
    /// </summary>
    public class ShowVO
    {
        public AdFormat Format { get; }

        public string Placement { get; }

        public Action<AdResultVO> Done { get; }

        public int Token { get; }

        public bool Displayed { get; set; }

        public bool Rewarded { get; set; }

        public AdRewardVO Reward { get; set; }

        public ShowVO(AdFormat format, string placement, Action<AdResultVO> done, int token)
        {
            Format = format;
            Placement = placement ?? string.Empty;
            Done = done;
            Token = token;
        }

        public override string ToString() => $"{Format}/{Placement}";
    }
}
