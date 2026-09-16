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
            /// Shows the interstitial for the placement given where the step is bound and holds
            /// the sequence until it ends: <c>.ToSequence&lt;IAdsService.Commands.ShowInterstitial&gt;("level_end")</c>.
            /// It releases on every outcome, the result handed on: an interstitial is a
            /// condition for nothing, and the flow behind it goes on whether it showed or not.
            /// </summary>
            [DebugOption("Ads", "Show interstitial", Argument = "debug")]
            public class ShowInterstitial : Command<string>
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute(string placement)
                {
                    Retain();
                    _ads.ShowInterstitial(placement, result => Release(result));
                }
            }
        }
    }
}
