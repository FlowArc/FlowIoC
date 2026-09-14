using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    public partial interface IHapticService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Plays the preset given where the step is bound:
            /// <c>.ToSequence&lt;IHapticService.Commands.Play&gt;(HapticPreset.Success)</c>. A step
            /// whose preset is decided at runtime is a game Command injecting IHapticService; a
            /// previous step may also hand the preset on with Release(preset).
            /// </summary>
            public class Play : Command<HapticPreset>
            {
                [Inject] private IHapticService _haptics { get; set; }

                public override void Execute(HapticPreset preset) => _haptics.Play(preset);
            }
        }
    }
}
