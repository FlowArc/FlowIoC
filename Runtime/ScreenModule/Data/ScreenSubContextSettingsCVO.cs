using System;
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Data
{
    /// <summary>
    /// What a Root says about a screen context it lists: whether it overrides the five values a
    /// scene is allowed to decide, and what they are. Load is not among them - where a prefab
    /// lives is the module's business, not the scene's.
    ///
    /// The values stay when Override is switched off, so switching it back on returns the edit
    /// rather than the declaration.
    /// </summary>
    [Serializable]
    public sealed class ScreenSubContextSettingsCVO : SubContextSettingsCVO
    {
        /// <summary>Whether the five values below replace what the screen context declares.</summary>
        public bool Override;

        /// <summary>Which screen manager this registration belongs to.</summary>
        public int ManagerId;

        /// <summary>How far up the stack the screen is drawn. A higher layer covers a lower one.</summary>
        public int Layer;

        /// <summary>What kind of surface this is - a screen in its own right, or a popup over one.</summary>
        public ScreenTag Tag;

        /// <summary>Whether the screen plays its own animation when it opens, instead of appearing.</summary>
        public bool HasShowAnimation;

        /// <summary>Whether the screen plays its own animation when it closes.</summary>
        public bool HasHideAnimation;

        /// <summary>A copy to edit, so the entry's own instance is replaced rather than changed.</summary>
        public ScreenSubContextSettingsCVO Copy() => (ScreenSubContextSettingsCVO) MemberwiseClone();
    }
}
