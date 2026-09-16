using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.Data.ValueObjects;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Hands consent on, given where the step is bound or by the step before it with
            /// Release(consent): <c>.ToSequence&lt;ResolveConsentCommand&gt;().ToSequence&lt;IAdsService.Commands.SetConsent&gt;()</c>.
            /// </summary>
            public class SetConsent : Command<AdsConsentVO>
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute(AdsConsentVO consent) => _ads.SetConsent(consent);
            }
        }
    }
}
