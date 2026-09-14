using FlowIoC.ScreenModule.Enums;

namespace Modules.LoadingModule.Shared.Constants
{
    /// <summary>
    /// What another module needs to know about this one without referencing its Runtime assembly.
    /// </summary>
    public static class LoadingConstants
    {
        /// <summary>
        /// The tag the module's two screens carry. A loading screen shows other loads, so it has
        /// to be in the pool before the set it shows begins: the boot loads this tag as its first
        /// step, and the open that follows is then a pooled open - on stage the same frame, with
        /// every later load drawn on it. A game whose GroupA is taken moves the tag here, once.
        /// </summary>
        public const ScreenTag SCREEN_TAG = ScreenTag.GroupA;
    }
}
