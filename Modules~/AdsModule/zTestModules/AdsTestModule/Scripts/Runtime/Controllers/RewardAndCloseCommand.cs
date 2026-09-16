#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>Reward + close: the fake reports the reward, then the close.</summary>
    public class RewardAndCloseCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        public override void Execute() => _provider.RewardAndClose();
    }
}

#endif
