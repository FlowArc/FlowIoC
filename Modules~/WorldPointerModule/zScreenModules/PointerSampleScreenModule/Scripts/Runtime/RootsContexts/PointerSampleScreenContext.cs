#if UNITY_EDITOR
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.WorldPointerModule.PointerSampleScreenModule.Controllers;
using Modules.WorldPointerModule.PointerSampleScreenModule.Signals;
using Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.RootsContexts
{
    /// <summary>
    /// The WorldPointer test module's display. Layer 3 is one of the ScreenManager's own-canvas
    /// layers, where UI that moves every frame belongs. It loads from Resources and its prefab
    /// sits in the module's Editor/Resources folder: the whole module is Editor-only, so nothing
    /// of it reaches a player build or an Addressables group.
    /// </summary>
    public class PointerSampleScreenContext : ScreenSubContext<PointerSampleScreenView, PointerSampleScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 3,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Resource("PointerSampleScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private PointerSampleScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            InjectionBinderCrossContext.Bind<PointerSampleScreenSignals>();
            _internalSignals = InjectionBinder.Bind<PointerSampleScreenInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.DisplaysShown).ToSequence<RegisterSampleDisplaysCommand>();
            CommandBinder.Bind(_internalSignals.DisplaysHidden).ToSequence<UnregisterSampleDisplaysCommand>();
        }
    }
}
#endif