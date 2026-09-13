using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Services;

namespace Modules.HapticModule.Controllers
{
    /// <summary>
    /// A step a game binds to the signal its settings toggle dispatches - a Signal&lt;bool&gt; -
    /// so the choice reaches the module without a Command of the game's own:
    /// <c>CommandBinder.Bind(_signals.HapticsToggled).ToSequence&lt;SetHapticsEnabledCommand&gt;()</c>.
    /// </summary>
    public class SetHapticsEnabledCommand : Command
    {
        [SignalParam] private bool _on { get; set; }

        [Inject] private IHapticService _haptics { get; set; }

        public override void Execute() => _haptics.SetEnabled(_on);
    }
}
