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
            /// Logs the event given where the step is bound:
            /// <c>.ToSequence&lt;IAnalyticsService.Commands.Log&gt;(new AnalyticsEventVO("settings_opened"))</c>.
            /// An event built from the payload or from state is a game Command injecting
            /// IAnalyticsService; a previous step may also hand the event on with Release(analyticsEvent).
            /// </summary>
            public class Log : Command<AnalyticsEventVO>
            {
                [Inject] private IAnalyticsService _analytics { get; set; }

                public override void Execute(AnalyticsEventVO analyticsEvent) => _analytics.Log(analyticsEvent);
            }
        }
    }
}
