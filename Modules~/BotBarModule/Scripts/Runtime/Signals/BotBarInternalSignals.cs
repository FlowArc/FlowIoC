using FlowIoC.BaseModule.Signals;

namespace Modules.BotBarModule.Signals
{
    /// <summary>
    /// What the module says to its own commands. No Incoming and no Outgoing: those halves describe
    /// a boundary, and nothing here crosses one.
    /// </summary>
    internal class BotBarInternalSignals : ISignalHolder
    {
        /// <summary>Every Setup in the scene has run, so every Connector has asked for its keys.</summary>
        public Signal Launched = new();

        /// <summary>A selection the module makes itself - the fall back to StartTab when the selected tab is locked.</summary>
        public Signal<string> SelectTab = new();
    }
}