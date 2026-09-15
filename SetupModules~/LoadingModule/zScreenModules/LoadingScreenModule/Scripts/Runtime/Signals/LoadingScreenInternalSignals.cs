using FlowIoC.BaseModule.Signals;
using UnityEngine;

namespace Modules.LoadingModule.LoadingScreenModule.Signals
{
    /// <summary>
    /// The screen talking to its own Commands: the art behind the bar is asked for once, at
    /// Launch, and announced when it lands. There is no Incoming and no Outgoing here - those two
    /// halves say what crosses a boundary, and an internal signal never does.
    /// </summary>
    internal class LoadingScreenInternalSignals : ISignalHolder
    {
        public Signal LoadBackground = new();
        public Signal<Sprite> BackgroundLoaded = new();
    }
}
