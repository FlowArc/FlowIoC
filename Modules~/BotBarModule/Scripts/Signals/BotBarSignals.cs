using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.Signals
{
    /// <summary>
    /// The bar's public surface. Incoming is what a game tells the bar; Outgoing is what the bar
    /// announces - to the game, and to its own screen through the shipped Connector.
    /// </summary>
    public class BotBarSignals : ISignalHolder
    {
        public BotBarSignalsIncoming Incoming = new();
        public BotBarSignalsOutgoing Outgoing = new();
    }

    public class BotBarSignalsIncoming
    {
        /// <summary>Bring the bar up on its StartTab. Announces nothing outward: the game's boot opens its home page itself.</summary>
        public Signal Open = new();

        /// <summary>Select a tab by its config key - a "go to the shop" button, Back-to-Home after a popup.</summary>
        public Signal<string> SelectTab = new();

        /// <summary>Key, locked. The game decided; the bar shows it and answers a tap on it with LockedTabTapped.</summary>
        public Signal<string, bool> SetLocked = new();

        /// <summary>Key, count. 0 clears.</summary>
        public Signal<string, int> SetBadge = new();

        /// <summary>Slide the bar back on.</summary>
        public Signal Show = new();

        /// <summary>Slide the bar off; the screen stays open.</summary>
        public Signal Hide = new();
    }

    public class BotBarSignalsOutgoing
    {
        public Signal Opened = new();

        /// <summary>Previous key, current key. Analytics reads every selection here, through a converter.</summary>
        public Signal<string, string> SelectionChanged = new();

        public Signal<string, BotBarTabState> TabStateChanged = new();
        public Signal<string, int> BadgeChanged = new();
        public Signal Shown = new();
        public Signal Hidden = new();

        /// <summary>A locked tab was tapped, or asked for through SelectTab. The game shows its toast.</summary>
        public Signal<string> LockedTabTapped = new();

        private readonly Dictionary<string, Signal> _selected = new();

        /// <summary>
        /// One tab's own announcement, by its config key. Created on the first ask and the same
        /// instance after, so a Connector asking in Setup and the Command dispatching later meet
        /// on one signal - which is what lets a Connector wire "the tab keyed shop" to the shop
        /// screen without an if. Named by hand: FlowFrameworkOrigin stamps a holder's fields, and
        /// these are not fields.
        /// </summary>
        public Signal Selected(string key)
        {
            if (!_selected.TryGetValue(key, out Signal signal))
            {
                signal = new Signal(name: $"BotBarSignals.Outgoing.Selected[{key}]");
                _selected[key] = signal;
            }

            return signal;
        }

        /// <summary>Every key somebody asked Selected for. Launch compares it with the config's keys.</summary>
        public IReadOnlyCollection<string> RequestedKeys => _selected.Keys;
    }
}