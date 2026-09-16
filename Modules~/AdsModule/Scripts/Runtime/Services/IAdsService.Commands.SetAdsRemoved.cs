using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// The "no ads" switch, bound behind the purchase step with the value given:
            /// <c>.ToSequence&lt;GrantPurchaseCommand&gt;().ToSequence&lt;IAdsService.Commands.SetAdsRemoved&gt;(true)</c>,
            /// and again at boot from the save.
            /// </summary>
            [DebugOption("Ads", "Remove ads", Argument = true)]
            [DebugOption("Ads", "Restore ads", Argument = false)]
            public class SetAdsRemoved : Command<bool>
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute(bool removed) => _ads.SetAdsRemoved(removed);
            }
        }
    }
}
