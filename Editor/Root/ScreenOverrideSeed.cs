#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// What the five override fields hold the first time someone ticks the override on a Root
    /// entry: the values the screen context declares, so the edit starts from the truth rather
    /// than from zero. An entry that has already been edited is left alone, which is what makes
    /// toggling the override off and on again non-destructive. When the declaration itself is all
    /// defaults the two cases produce the same result, so no extra "already seeded" flag is
    /// stored.
    /// </summary>
    internal class ScreenOverrideSeed
    {
        internal SubContextData Apply(SubContextData data, ScreenCVO declaration)
        {
            if (declaration == null || !IsUntouched(data))
                return data;

            data.ScreenManagerId = declaration.ManagerId;
            data.ScreenLayer = declaration.Layer;
            data.ScreenTag = declaration.Tag;
            data.ScreenHasShowAnimation = declaration.HasShowAnimation;
            data.ScreenHasHideAnimation = declaration.HasHideAnimation;

            return data;
        }

        private bool IsUntouched(SubContextData data)
        {
            return data.ScreenManagerId == 0
                   && data.ScreenLayer == 0
                   && data.ScreenTag == ScreenTag.Default
                   && !data.ScreenHasShowAnimation
                   && !data.ScreenHasHideAnimation;
        }
    }
}
#endif
