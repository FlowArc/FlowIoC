using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Data
{
    /// <summary>
    /// Everything a screen declares about itself, written in its ScreenSubContext. This is the
    /// config data that used to live in a CD_Screen asset; it is a CVO because it is authored and
    /// constant at runtime, and it lives in code because the context is the screen's one
    /// declaration.
    /// </summary>
    public class ScreenCVO
    {
        /// <summary>The ScreenManager this screen opens in. 0 is the only manager most games have.</summary>
        public int ManagerId = 0;

        /// <summary>The layer this screen opens in unless OpenInLayer overrides it.</summary>
        public int Layer = 0;

        /// <summary>Groups screens for bulk load, hide and unload.</summary>
        public ScreenTag Tag = ScreenTag.Default;

        /// <summary>Required. A screen whose Load has no key is rejected at registration.</summary>
        public ScreenLoadCVO Load;

        /// <summary>Whether the module waits for PlayShowAnimation to report ShowCompleted.</summary>
        public bool HasShowAnimation;

        /// <summary>Whether the module waits for PlayHideAnimation to report HideCompleted.</summary>
        public bool HasHideAnimation;
    }
}
