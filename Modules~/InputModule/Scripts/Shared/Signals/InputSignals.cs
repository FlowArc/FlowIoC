using FlowIoC.BaseModule.Signals;
using UnityEngine;

namespace Modules.InputModule.Shared.Signals
{
    public class InputSignals : ISignalHolder
    {
        public InputSignalsIncoming Incoming = new();
        public InputSignalsOutgoing Outgoing = new();
    }

    public class InputSignalsIncoming
    {
        /// <summary>
        /// Turns one action map of the module's asset on or off by name. A game silences gameplay
        /// input while a screen is open by disabling the map rather than by ignoring what arrives.
        /// </summary>
        public Signal<string, bool> SetActionMapEnabled = new();
    }

    public class InputSignalsOutgoing
    {
        /// <summary>Where the pointer went down, in screen coordinates.</summary>
        public Signal<Vector2> PointerPressed = new();

        /// <summary>Where the pointer moved to while it is down. Nothing is announced while it is up.</summary>
        public Signal<Vector2> PointerDragged = new();

        /// <summary>Where the pointer came up.</summary>
        public Signal<Vector2> PointerReleased = new();
    }
}
