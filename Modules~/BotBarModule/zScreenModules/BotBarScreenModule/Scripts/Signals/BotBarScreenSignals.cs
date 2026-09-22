using FlowIoC.BaseModule.Signals;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.BotBarScreenModule.Signals
{
    /// <summary>
    /// What the bar's screen accepts - the System's announcements, carried here by
    /// BotBarScreenConnectorSubContext - and the one thing it announces.
    /// </summary>
    public class BotBarScreenSignals : ISignalHolder
    {
        public BotBarScreenSignalsIncoming Incoming = new();
        public BotBarScreenSignalsOutgoing Outgoing = new();
    }

    public class BotBarScreenSignalsIncoming
    {
        public Signal Open = new();

        /// <summary>Previous key, current key.</summary>
        public Signal<string, string> ApplySelection = new();

        public Signal<string, BotBarTabState> ApplyTabState = new();
        public Signal<string, int> ApplyBadge = new();
        public Signal Show = new();
        public Signal Hide = new();
    }

    public class BotBarScreenSignalsOutgoing
    {
        /// <summary>A tab was tapped, locked or not. The System decides what a tap means.</summary>
        public Signal<string> TabTapped = new();
    }
}