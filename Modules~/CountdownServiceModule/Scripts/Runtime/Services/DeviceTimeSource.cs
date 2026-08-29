using System;

namespace Modules.CountdownServiceModule.Services
{
    /// <summary>
    /// The time source the module uses unless a game binds another: the device's own clock. It
    /// needs no network and is ready immediately, which is what lets the module work as soon as
    /// it is installed. A player who moves the device clock moves every countdown with it, so a
    /// game that cares about that binds a source of its own.
    /// </summary>
    public class DeviceTimeSource : ITimeSource
    {
        public bool IsReady => true;

        public DateTime UtcNow => DateTime.UtcNow;

        public void Prepare(Action<bool> onPrepared) => onPrepared?.Invoke(true);
    }
}
