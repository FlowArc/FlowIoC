using System;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// One sub-context as a Root lists it. The screen fields are the exception to this struct
    /// being about contexts in general: they are the configuration a screen context declares in
    /// code, which the Root may override for its own registration. They are prefixed so another
    /// kind of sub-context can add its own without a collision.
    /// </summary>
    [Serializable]
    public struct SubContextData
    {
        public string ContextFullName;
        public string ContextName;

        public bool AutoSetup;
        public bool IsTest;

        /// <summary>
        /// Whether the five screen fields below replace what the screen context declares. Off on
        /// every entry that predates the feature, so an untouched scene keeps its behaviour.
        /// </summary>
        public bool OverrideScreen;

        /// <summary>Which screen manager this registration belongs to.</summary>
        public int ScreenManagerId;

        /// <summary>How far up the stack the screen is drawn. A higher layer covers a lower one.</summary>
        public int ScreenLayer;

        /// <summary>What kind of surface this is - a screen in its own right, or a popup over one.</summary>
        public ScreenTag ScreenTag;

        /// <summary>Whether the screen plays its own animation when it opens, instead of appearing.</summary>
        public bool ScreenHasShowAnimation;

        /// <summary>Whether the screen plays its own animation when it closes.</summary>
        public bool ScreenHasHideAnimation;
    }
}