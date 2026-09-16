using Modules.AdsModule.Enums;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>One format's ad as the module knows it: loading, loaded, or waiting to try again.</summary>
    public class AdSlotVO
    {
        public AdFormat Format { get; }

        public AdLoadState State { get; set; }

        /// <summary>How many loads in a row have failed; reset when one lands.</summary>
        public int Attempt { get; set; }

        public AdSlotVO(AdFormat format) => Format = format;
    }
}
