#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.Signals
{
    /// <summary>The buttons, and the line the label shows.</summary>
    internal class BotBarTestInternalSignals : ISignalHolder
    {
        public Signal SelectShop = new();
        public Signal<bool> SetClanLocked = new();
        public Signal IncrementShopBadge = new();
        public Signal ClearShopBadge = new();
        public Signal<bool> SetBarShown = new();
        public Signal<string> Note = new();
        public Signal<string> StatusChanged = new();
    }
}
#endif
