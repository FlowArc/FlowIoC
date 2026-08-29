using System;

namespace Modules.CountdownServiceModule.Services
{
    /// <summary>
    /// Where the module reads the current time from. The implementation it ships with answers
    /// from the device clock, which needs nothing and is ready the moment it is asked. A game
    /// that wants a clock the player cannot move - a server time endpoint, a platform service -
    /// binds its own implementation in its place, and every countdown follows without changing.
    /// </summary>
    public interface ITimeSource
    {
        /// <summary>True once <see cref="UtcNow"/> can be trusted.</summary>
        bool IsReady { get; }

        /// <summary>The current UTC time. Only meaningful while <see cref="IsReady"/> is true.</summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Asks the source to make itself ready, and reports whether it managed to. The callback
        /// may run before this method returns - the device clock has nothing to wait for.
        /// </summary>
        void Prepare(Action<bool> onPrepared);
    }
}
