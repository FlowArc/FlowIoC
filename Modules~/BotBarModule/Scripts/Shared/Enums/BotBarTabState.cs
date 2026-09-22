namespace Modules.BotBarModule.Shared.Enums
{
    /// <summary>What a tab is right now. Numbered: Unity serializes an enum as its int.</summary>
    public enum BotBarTabState
    {
        Open = 0,
        Locked = 1,

        /// <summary>Unlocked since the bar was last tapped there: carries the NEW mark until its first tap.</summary>
        NewlyUnlocked = 2,
    }
}
