#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Signals
{
    /// <summary>
    /// What the screen's Mediator says to its own commands. A Mediator injects nothing but its
    /// View, so registering the screen's layers with the service is a Command's, and the Mediator
    /// only says when the screen came up and went down.
    /// </summary>
    internal class WorldPointerSampleScreenInternalSignals : ISignalHolder
    {
        /// <summary>The screen has shown: its layers become the displays of the sample's ids.</summary>
        public Signal<WorldPointerSampleScreenView> DisplaysShown = new();

        /// <summary>The screen has hidden: its layers stop being displays, and the targets wait.</summary>
        public Signal<WorldPointerSampleScreenView> DisplaysHidden = new();
    }
}
#endif