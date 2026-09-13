#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Services;

namespace Modules.HapticModule.HapticTestModule.Controllers
{
    /// <summary>What a settings screen's Command does with its haptics toggle.</summary>
    public class SetTestHapticsEnabledCommand : Command
    {
        [SignalParam] private bool _on { get; set; }

        [Inject] private IHapticService _haptics { get; set; }

        public override void Execute() => _haptics.SetEnabled(_on);
    }
}

#endif
