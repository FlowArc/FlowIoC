using System;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.RootsContexts
{
    /// <summary>
    /// The context a screen module derives from - its declaration. It binds the view to the
    /// mediator, and through ScreenSubContextBase hands the service a ScreenCVO saying where the
    /// prefab lives and how the screen behaves, together with itself as the owner: the service
    /// registers the loaded view against this context, so the mediator comes from here even
    /// though the instance is parented under a ScreenRoot layer.
    ///
    /// A screen context is a sub-context of the module it lives in, listed in that module's Root.
    /// It is not a ScreenRoot concern, and it never derives from BaseScreenContext, which is the
    /// base for the context that owns a ScreenManager.
    /// </summary>
    public abstract class ScreenSubContext<TView, TMediator> : ScreenSubContextBase
        where TView : ScreenView
        where TMediator : IMediator
    {
        internal override Type ScreenViewType => typeof(TView);

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<TView>().To<TMediator>();
        }
    }
}