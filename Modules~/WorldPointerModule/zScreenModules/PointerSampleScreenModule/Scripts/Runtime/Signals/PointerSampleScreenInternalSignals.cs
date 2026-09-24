#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;
using Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Signals
{
    /// <summary>
    /// What the screen's Mediator says to its own commands. A Mediator injects nothing but its
    /// View, so registering the screen's displays with the service is a Command's, and the Mediator
    /// only says when the screen came up and went down.
    /// </summary>
    internal class PointerSampleScreenInternalSignals : ISignalHolder
    {
        /// <summary>The screen has shown: its panels become the displays of the sample's channels.</summary>
        public Signal<PointerSampleScreenView> DisplaysShown = new();

        /// <summary>The screen has hidden: the channels lose their displays, and the targets wait.</summary>
        public Signal DisplaysHidden = new();
    }
}
#endif