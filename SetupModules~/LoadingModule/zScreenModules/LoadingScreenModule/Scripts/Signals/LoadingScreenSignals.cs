using FlowIoC.BaseModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingScreenModule.Signals
{
    public class LoadingScreenSignals : ISignalHolder
    {
        public LoadingScreenSignalsIncoming Incoming = new();
        public LoadingScreenSignalsOutgoing Outgoing = new();
    }

    public class LoadingScreenSignalsIncoming
    {
        /// <summary>A Fullscreen set began: open the screen with this snapshot.</summary>
        public Signal<LoadingSetStatusRVO> Open = new();

        /// <summary>A set changed. The screen applies it if it is the set on show.</summary>
        public Signal<LoadingSetStatusRVO> Apply = new(hideCommandLog: true);

        /// <summary>A set completed: close the screen if it is showing that set.</summary>
        public Signal<string> Close = new();

        /// <summary>A set failed at a step: show the failed state with the retry button.</summary>
        public Signal<string, string> ShowFailed = new();
    }

    public class LoadingScreenSignalsOutgoing
    {
        /// <summary>The player asked to retry the named set.</summary>
        public Signal<string> RetryClicked = new();
    }
}