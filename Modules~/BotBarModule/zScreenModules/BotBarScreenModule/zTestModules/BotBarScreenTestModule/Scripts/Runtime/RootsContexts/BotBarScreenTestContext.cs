#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.BotBarModule.BotBarScreenModule.Signals;

namespace Modules.BotBarModule.BotBarScreenModule.BotBarScreenTestModule.RootsContexts
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// The bar is opened through its own Open signal rather than the screen service, so the
    /// opening Command fills it from the stand-in assets this Root files - clan locked, shop with
    /// a badge - and the prefab is seen as the game will show it.
    /// </summary>
    [ExcludeFromContextWindow]
    public class BotBarScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
            InjectionBinderCrossContext.GetInstance<BotBarScreenSignals>().Incoming.Open.Dispatch();
        }
    }
}
#endif