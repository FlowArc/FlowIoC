#if UNITY_EDITOR
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// What the five override values hold the first time someone ticks the override on a Root
    /// entry: the values the screen context declares, so the edit starts from the truth rather
    /// than from zero. Settings that have already been edited are left alone, which is what makes
    /// toggling the override off and on again non-destructive. When the declaration itself is all
    /// defaults the two cases produce the same result, so no extra "already seeded" flag is
    /// stored.
    ///
    /// Always hands back a new instance, never the one it was given: the entry's own settings are
    /// replaced, not changed, so Undo sees the edit.
    /// </summary>
    internal class ScreenOverrideSeed
    {
        internal ScreenSubContextSettingsCVO Apply(ScreenSubContextSettingsCVO settings, ScreenCVO declaration)
        {
            ScreenSubContextSettingsCVO seeded = settings?.Copy() ?? new ScreenSubContextSettingsCVO();

            if (declaration == null || !IsUntouched(seeded))
                return seeded;

            seeded.ManagerId = declaration.ManagerId;
            seeded.Layer = declaration.Layer;
            seeded.Tag = declaration.Tag;
            seeded.HasShowAnimation = declaration.HasShowAnimation;
            seeded.HasHideAnimation = declaration.HasHideAnimation;

            return seeded;
        }

        private bool IsUntouched(ScreenSubContextSettingsCVO settings)
        {
            return settings.ManagerId == 0
                   && settings.Layer == 0
                   && settings.Tag == ScreenTag.Default
                   && !settings.HasShowAnimation
                   && !settings.HasHideAnimation;
        }
    }
}
#endif
