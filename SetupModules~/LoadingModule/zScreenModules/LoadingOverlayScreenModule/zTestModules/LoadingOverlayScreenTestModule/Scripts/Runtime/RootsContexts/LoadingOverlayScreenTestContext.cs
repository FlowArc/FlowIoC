#if UNITY_EDITOR
using Modules.LoadingModule.LoadingOverlayScreenModule.ViewsMediators;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.LoadingOverlayScreenTestModule.RootsContexts
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    public class LoadingOverlayScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
			_screenService.Open<LoadingOverlayScreenView>().Show();

        }
    }
}
#endif
