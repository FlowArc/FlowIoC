#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>Fail to display: the fake reports a display failure for the ad on screen.</summary>
    public class FailToDisplayCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        public override void Execute() => _provider.FailToDisplay();
    }
}

#endif
