#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>The Silent show toggle, handed to the fake provider.</summary>
    public class SetSilentShowCommand : Command
    {
        [Inject] private FakeAdsProvider _provider { get; set; }

        [SignalParam] private bool _silent { get; set; }

        public override void Execute() => _provider.SilentShow = _silent;
    }
}

#endif
