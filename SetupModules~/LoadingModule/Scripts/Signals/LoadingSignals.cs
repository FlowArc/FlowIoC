using FlowIoC.BaseModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.Signals
{
    public class LoadingSignals : ISignalHolder
    {
        public LoadingSignalsIncoming Incoming = new();
        public LoadingSignalsOutgoing Outgoing = new();
    }

    public class LoadingSignalsIncoming
    {
        /// <summary>Begin a set from a Connector, for a flow that has no ILoadingService of its own.</summary>
        public Signal<string> Begin = new();
    }

    public class LoadingSignalsOutgoing
    {
        /// <summary>A Fullscreen set began. The Connector opens the loading screen with this snapshot.</summary>
        public Signal<LoadingSetStatusRVO> FullscreenBegan = new();

        /// <summary>An Overlay set began. The Connector opens the overlay with this snapshot.</summary>
        public Signal<LoadingSetStatusRVO> OverlayBegan = new();

        /// <summary>Any set, any presentation: progress, message or detail changed.</summary>
        public Signal<LoadingSetStatusRVO> SetChanged = new(hideCommandLog: true);

        public Signal<string> SetCompleted = new();

        /// <summary>The set, and the step that failed it.</summary>
        public Signal<string, string> SetFailed = new();
    }
}