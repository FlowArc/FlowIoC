using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;

namespace Modules.HapticModule.Controllers
{
    /// <summary>The one decision a play involves - off, or nothing to play - and then the platform.</summary>
    internal class PlayPresetCommand : Command
    {
        [SignalParam] private HapticPreset _preset { get; set; }

        [Inject] private IHapticModel _model { get; set; }
        [Inject] private IHapticPlayer _player { get; set; }

        public override void Execute()
        {
            if (!_model.IsEnabled || _preset == HapticPreset.None)
                return;

            _player.Play(_preset);
        }
    }
}
