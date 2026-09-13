using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The platform behind a preset. The context binds one implementation by platform, the way
    /// CounterModule binds ITimeSource to DeviceTimeSource, and the commands never learn which.
    /// </summary>
    public interface IHapticPlayer
    {
        void Initialize();

        void Play(HapticPreset preset);

        void Stop();
    }
}
