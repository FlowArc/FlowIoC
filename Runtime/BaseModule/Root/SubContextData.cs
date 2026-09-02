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

        public int ScreenManagerId;
        public int ScreenLayer;
        public ScreenTag ScreenTag;
        public bool ScreenHasShowAnimation;
        public bool ScreenHasHideAnimation;
    }
}