#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>The Loads fail toggle, handed to the fake provider.</summary>
    public class SetLoadsFailCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        [SignalParam] private bool _fail { get; set; }

        public override void Execute() => _provider.LoadsFail = _fail;
    }
}

#endif
