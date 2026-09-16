using FlowIoC.BaseModule.Signals;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Signals
{
    /// <summary>
    /// What the module announces to the whole game. There is no Incoming: the way into a Service
    /// is its interface and the steps under IAdsService.Commands.
    /// </summary>
    public class AdsSignals : ISignalHolder
    {
        public AdsSignalsOutgoing Outgoing = new();
    }

    public class AdsSignalsOutgoing
    {
        /// <summary>An ad of the format became showable (true, a load landed) or stopped being (false, a show began). What a watch-ad button follows.</summary>
        public Signal<AdFormat, bool> ReadyChanged = new();

        /// <summary>An ad is on screen: format and placement. Pause the game, mute the audio.</summary>
        public Signal<AdFormat, string> Opened = new();

        /// <summary>The ad left the screen: format and placement. Whoever asked has already been answered.</summary>
        public Signal<AdFormat, string> Closed = new();

        /// <summary>The SDK could not show what was asked: format, placement, reason.</summary>
        public Signal<AdFormat, string, string> Failed = new();

        /// <summary>What an impression paid. A Connector carries it to the game's analytics logging.</summary>
        public Signal<AdRevenueVO> RevenuePaid = new();
    }
}
