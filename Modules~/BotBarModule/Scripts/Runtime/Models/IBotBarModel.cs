using System.Collections.Generic;
using Modules.BotBarModule.Shared.Data.UnityObjects;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.Models
{
    /// <summary>The bar's state and the rules that keep it valid. It dispatches nothing and listens to nothing.</summary>
    public interface IBotBarModel
    {
        CD_BotBar Config { get; }
        IReadOnlyList<BotBarTabRVO> Tabs { get; }
        string Selected { get; }

        /// <summary>The config's StartTab when a tab carries it, the first tab otherwise; null with no tab at all.</summary>
        string StartTab { get; }

        bool IsShown { get; }

        bool HasTab(string key);

        /// <summary>The tab, or null when no tab carries the key.</summary>
        BotBarTabRVO GetTab(string key);

        /// <summary>Selects the tab and hands back the key selected before. An unknown key changes nothing.</summary>
        string Select(string key);

        /// <summary>NewlyUnlocked becomes Open. True when it did.</summary>
        bool MarkSeen(string key);

        /// <summary>True when the state changed, with the state it is now.</summary>
        bool SetLocked(string key, bool locked, out BotBarTabState state);

        /// <summary>Clamped at 0. True when the count changed.</summary>
        bool SetBadge(string key, int count);

        /// <summary>True when it changed.</summary>
        bool SetShown(bool shown);
    }
}
