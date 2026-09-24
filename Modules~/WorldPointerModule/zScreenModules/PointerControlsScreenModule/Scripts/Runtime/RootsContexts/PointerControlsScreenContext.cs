#if UNITY_EDITOR
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.WorldPointerModule.PointerControlsScreenModule.Signals;
using Modules.WorldPointerModule.PointerControlsScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.PointerControlsScreenModule.RootsContexts
{
    /// <summary>
    /// The WorldPointer test scene's buttons, as a screen: an overlay canvas exists only through
    /// the ScreenManager, test scenes included where they can. Layer 5 sits above the pointer
    /// layers. Editor-only and loaded from Resources, like the sample screen beside it.
    /// </summary>
    public class PointerControlsScreenContext : ScreenSubContext<PointerControlsScreenView,
        PointerControlsScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 5,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Resource("PointerControlsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        public override void SignalBindings()
        {
            base.SignalBindings();

            InjectionBinderCrossContext.Bind<PointerControlsScreenSignals>();
        }
    }
}
#endif