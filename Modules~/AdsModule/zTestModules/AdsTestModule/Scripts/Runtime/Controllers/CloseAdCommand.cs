#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>Close: the fake reports the close alone.</summary>
    public class CloseAdCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        public override void Execute() => _provider.Close();
    }
}

#endif
