using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;

namespace Modules.HapticModule.Controllers
{
    /// <summary>
    /// A step a game binds in a sequence of its own, the preset given where the step is bound:
    /// <c>.ToSequence&lt;HapticServicePlayCommand&gt;(HapticPreset.Success)</c>. The flow then reads from
    /// the Context - which preset, and after which step - without opening a Command to find the
    /// Play call. A step whose preset is decided at runtime is a game Command injecting
    /// IHapticService; a previous step may also hand the preset on with Release(preset).
    /// </summary>
    public class HapticServicePlayCommand : Command<HapticPreset>
    {
        [Inject] private IHapticService _haptics { get; set; }

        public override void Execute(HapticPreset preset) => _haptics.Play(preset);
    }
}
