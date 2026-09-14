using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.HapticModule.Services
{
    public partial interface IHapticService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Bound to the signal a settings toggle dispatches - a Signal&lt;bool&gt; - so the choice
            /// reaches the module without a Command of the game's own:
            /// <c>CommandBinder.Bind(_signals.HapticsToggled).ToSequence&lt;IHapticService.Commands.SetEnabled&gt;()</c>.
            /// </summary>
            public class SetEnabled : Command
            {
                [SignalParam] private bool _on { get; set; }

                [Inject] private IHapticService _haptics { get; set; }

                public override void Execute() => _haptics.SetEnabled(_on);
            }
        }
    }
}
