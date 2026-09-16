#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>The toggle's value, read off the signal and handed to the service - what a game writes when the value is not known at bind time.</summary>
    public class SetTestAdsRemovedCommand : Command
    {
        [Inject] private IAdsService _ads { get; set; }

        [SignalParam] private bool _removed { get; set; }

        public override void Execute() => _ads.SetAdsRemoved(_removed);
    }
}

#endif
