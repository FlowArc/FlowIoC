using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// Reads the screen override an entry stored in its flat fields before it moved into the
    /// entry's settings. Temporary, and the one place BaseModule still names a screen type: it goes
    /// with the legacy fields on SubContextData.
    /// </summary>
    internal class LegacyScreenOverride
    {
        /// <summary>
        /// True when the entry held old values and now holds them as settings. An entry that
        /// already has settings, or whose old fields are all at their defaults - every entry that
        /// is not a screen, and every screen never overridden - is left as it is.
        /// </summary>
        internal bool TryMigrate(ref SubContextData entry)
        {
            if (entry.Settings != null || !HasLegacyValues(entry))
                return false;

            entry.Settings = new ScreenSubContextSettingsCVO
            {
                Override = entry.LegacyOverrideScreen,
                ManagerId = entry.LegacyScreenManagerId,
                Layer = entry.LegacyScreenLayer,
                Tag = (ScreenTag) entry.LegacyScreenTag,
                HasShowAnimation = entry.LegacyScreenHasShowAnimation,
                HasHideAnimation = entry.LegacyScreenHasHideAnimation
            };

            entry.LegacyOverrideScreen = false;
            entry.LegacyScreenManagerId = 0;
            entry.LegacyScreenLayer = 0;
            entry.LegacyScreenTag = 0;
            entry.LegacyScreenHasShowAnimation = false;
            entry.LegacyScreenHasHideAnimation = false;

            return true;
        }

        private bool HasLegacyValues(SubContextData entry)
        {
            return entry.LegacyOverrideScreen
                   || entry.LegacyScreenManagerId != 0
                   || entry.LegacyScreenLayer != 0
                   || entry.LegacyScreenTag != 0
                   || entry.LegacyScreenHasShowAnimation
                   || entry.LegacyScreenHasHideAnimation;
        }
    }
}
