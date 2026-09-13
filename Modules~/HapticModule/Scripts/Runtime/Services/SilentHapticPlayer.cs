using FlowIoC.ConsoleModule;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The Editor, standalone and the web: nothing vibrates, so a play is a line on the module's
    /// channel and a flow can be followed in the Flow Console without a device.
    /// </summary>
    public class SilentHapticPlayer : IHapticPlayer
    {
        public void Initialize()
        {
        }

        public void Play(HapticPreset preset) =>
            FlowLogger.Log($"Play - {preset} (no haptics on this platform)");

        public void Stop()
        {
        }
    }
}
