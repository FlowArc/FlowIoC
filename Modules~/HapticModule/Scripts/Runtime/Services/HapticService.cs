using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using Modules.HapticModule.Signals;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through the internal signals, so each one is a
    /// step the Flow Console shows and the decisions - off, None - are taken in a Command.
    /// </summary>
    public class HapticService : IHapticService
    {
        [Inject] private IHapticModel _model { get; set; }
        [InjectSignal] private HapticInternalSignals _signals { get; set; }

        public void Play(HapticPreset preset) => _signals.Play.Dispatch(preset);

        public bool IsEnabled() => _model.IsEnabled;

        public void SetEnabled(bool on) => _signals.SetEnabled.Dispatch(on);
    }
}
