using System;
using UnityEngine;

namespace Modules.BotBarModule.Shared.Data.ValueObjects
{
    /// <summary>One tab as authored. The key is its identity; the title is only what it shows.</summary>
    [Serializable]
    public class BotBarTabCVO
    {
        [Tooltip("The tab's identity: what a Connector's Outgoing.Selected(key) and Incoming.SelectTab name. Never shown; renaming the Title changes nothing here.")]
        public string Key;

        public string Title;
        public Sprite IdleIcon;
        public Sprite SelectedIcon;

        [Tooltip("Shown on the tab while it is locked - \"Lv 5\". Empty shows nothing.")]
        public string LockedLabel;
    }
}
