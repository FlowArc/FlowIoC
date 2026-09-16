#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>Answers the fake provider's held Initialize with the button's answer.</summary>
    public class AnswerProviderCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        [SignalParam] private bool _ready { get; set; }

        public override void Execute() => _provider.Answer(_ready);
    }
}

#endif
