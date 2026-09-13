#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using FlowIoC.ConsoleModule;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// iOS's side of a preset: UIKit's feedback generators, through Plugins/iOS/FlowHaptics.mm.
    /// A preset is a system haptic, so it cannot be stopped - and none lasts half a second.
    /// </summary>
    public class IosHapticPlayer : IHapticPlayer
    {
        // The externs are static because DllImport allows nothing else.
        [DllImport("__Internal")] private static extern bool FlowHapticsInitialize();
        [DllImport("__Internal")] private static extern void FlowHapticsTrigger(int preset);

        private bool _ready;

        public void Initialize()
        {
            _ready = FlowHapticsInitialize();
            FlowLogger.Log(FlowModule.HapticModule, $"Initialize - IosHapticPlayer | ready={_ready}");
        }

        public void Play(HapticPreset preset)
        {
            if (!_ready || preset == HapticPreset.None)
                return;

            FlowHapticsTrigger((int) preset);
        }

        public void Stop()
        {
        }
    }
}
#endif
