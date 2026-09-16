#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.AnalyticsTestModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Controllers
{
    /// <summary>Answers the recording provider's held Initialize callback with the button's answer.</summary>
    public class AnswerProviderCommand : Command
    {
        [Inject] private RecordingAnalyticsProvider _provider { get; set; }

        [SignalParam] private bool _ready { get; set; }

        public override void Execute() => _provider.Answer(_ready);
    }
}

#endif
