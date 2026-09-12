using FlowIoC.BaseModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.Signals
{
    public class LoadingOverlayScreenSignals : ISignalHolder
    {
        public LoadingOverlayScreenSignalsIncoming Incoming = new();
        public LoadingOverlayScreenSignalsOutgoing Outgoing = new();
    }

    public class LoadingOverlayScreenSignalsIncoming
    {
        /// <summary>An Overlay set began: open the overlay with this snapshot.</summary>
        public Signal<LoadingSetStatusRVO> Open = new();

        /// <summary>A set changed. The overlay applies it if it is the set on show.</summary>
        public Signal<LoadingSetStatusRVO> Apply = new(hideCommandLog: true);

        /// <summary>A set ended, completed or failed: close the overlay if it is showing that set.</summary>
        public Signal<string> Close = new();
    }

    /// <summary>The overlay announces nothing; the half stays so the holder keeps its shape.</summary>
    public class LoadingOverlayScreenSignalsOutgoing
    {
    }
}