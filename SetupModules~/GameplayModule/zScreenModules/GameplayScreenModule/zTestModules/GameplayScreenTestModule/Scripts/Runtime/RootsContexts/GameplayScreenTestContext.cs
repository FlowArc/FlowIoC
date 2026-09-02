#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;

namespace Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.RootsContexts
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    public class GameplayScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
            _screenService.Open<GameplayScreenView>().Show();
        }
    }
}
#endif