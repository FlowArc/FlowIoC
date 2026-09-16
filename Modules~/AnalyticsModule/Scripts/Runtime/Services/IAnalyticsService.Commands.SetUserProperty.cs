using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AnalyticsModule.Services
{
    public partial interface IAnalyticsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Sets the property given where the step is bound:
            /// <c>.ToSequence&lt;IAnalyticsService.Commands.SetUserProperty&gt;("build", "beta")</c>,
            /// or handed on by the previous step with Release(name, value).
            /// </summary>
            public class SetUserProperty : Command<string, string>
            {
                [Inject] private IAnalyticsService _analytics { get; set; }

                public override void Execute(string name, string value) => _analytics.SetUserProperty(name, value);
            }
        }
    }
}
