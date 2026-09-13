#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;

namespace Modules.HapticModule.HapticTestModule.Controllers
{
    /// <summary>What a game module's Command does at the moment it decided something happened.</summary>
    public class PlayTestHapticCommand : Command
    {
        [SignalParam] private HapticPreset _preset { get; set; }

        [Inject] private IHapticService _haptics { get; set; }

        public override void Execute() => _haptics.Play(_preset);
    }
}

#endif
