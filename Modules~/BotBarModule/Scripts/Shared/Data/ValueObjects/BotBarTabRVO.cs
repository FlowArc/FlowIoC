using System;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.Shared.Data.ValueObjects
{
    /// <summary>One tab's state during play: produced from the config, never persisted.</summary>
    [Serializable]
    public class BotBarTabRVO
    {
        public string Key;
        public BotBarTabState State;
        public int Badge;
    }
}
