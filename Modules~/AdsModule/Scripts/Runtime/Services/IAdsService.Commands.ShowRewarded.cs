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
            /// Shows the rewarded ad for the placement given where the step is bound and holds
            /// the sequence until it ends: <c>.ToSequence&lt;IAdsService.Commands.ShowRewarded&gt;("chest")</c>.
            /// The reward earned releases with the result; every other outcome - dismissed, not
            /// ready, failed - stops the sequence, so the grant bound after this step needs no
            /// check of its own. A game that wants a "no ad right now" popup on the other
            /// outcomes writes a Command that injects IAdsService and decides both paths in the
            /// callback.
            /// </summary>
            [DebugOption("Ads", "Show rewarded", Argument = "debug")]
            public class ShowRewarded : Command<string>
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute(string placement)
                {
                    Retain();

                    _ads.ShowRewarded(placement, result =>
                    {
                        if (result.IsRewarded)
                            Release(result);
                        else
                            Stop();
                    });
                }
            }
        }
    }
}
