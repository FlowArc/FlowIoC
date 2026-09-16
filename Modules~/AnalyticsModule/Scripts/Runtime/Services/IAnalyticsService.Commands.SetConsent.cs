using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Data.ValueObjects;

namespace Modules.AnalyticsModule.Services
{
    public partial interface IAnalyticsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Hands the consent on. Bound with a value where a game grants everything,
            /// <c>.ToSequence&lt;IAnalyticsService.Commands.SetConsent&gt;(new AnalyticsConsentVO(true))</c>,
            /// or after the step that resolved it, which hands it on with Release(consent).
            /// </summary>
            public class SetConsent : Command<AnalyticsConsentVO>
            {
                [Inject] private IAnalyticsService _analytics { get; set; }

                public override void Execute(AnalyticsConsentVO consent) => _analytics.SetConsent(consent);
            }
        }
    }
}
