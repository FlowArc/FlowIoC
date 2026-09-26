#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.LoadingModule.LoadingOverlayScreenModule.Signals;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using Modules.LoadingModule.Shared.Enums;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.LoadingOverlayScreenTestModule.RootsContexts
{
    /// <summary>
    /// Opens the overlay the way the Connector does when an Overlay set begins - through the
    /// screen's own Open signal, with a set that is running - so the scene shows the overlay filled,
    /// on its layer, with nothing of the game's flow around it. The production context, listed as a
    /// sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    public class LoadingOverlayScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();

            InjectionBinderCrossContext.GetInstance<LoadingOverlayScreenSignals>().Incoming.Open.Dispatch(
                new LoadingSetStatusRVO
                {
                    Set = "Sample",
                    Presentation = LoadingPresentation.Overlay,
                    State = LoadingSetState.Running,
                    Message = "Loading..."
                });
        }
    }
}
#endif
