using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Signals;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.RootsContexts
{
    /// <summary>
    /// The context a screen module derives from - its one declaration. It binds the view to the
    /// mediator, and in Setup hands the service a ScreenCVO saying where the prefab lives and how
    /// the screen behaves, together with itself as the owner: the service registers the loaded
    /// view against this context, so the mediator comes from here even though the instance is
    /// parented under a ScreenRoot layer.
    ///
    /// A screen context is a sub-context of the module it lives in, listed in that module's Root.
    /// It is not a ScreenRoot concern, and it never derives from BaseScreenContext, which is the
    /// base for the context that owns a ScreenManager.
    /// </summary>
    public abstract class ScreenSubContext<TView, TMediator> : Context
        where TView : ScreenView
        where TMediator : IMediator
    {
        protected IScreenService _screenService;

        private ScreenServiceInternalSignals _internalSignals;

        /// <summary>
        /// Where the prefab comes from and how the screen behaves. Abstract rather than defaulted:
        /// a context that does not say where its prefab lives should not compile, instead of
        /// failing at the first Open.
        /// </summary>
        protected abstract ScreenCVO Screen { get; }

        protected override void CoreBindings()
        {
            base.CoreBindings();

            _screenService = InjectionBinderCrossContext.GetInstance<IScreenService>();
            _internalSignals = InjectionBinderCrossContext.GetInstance<ScreenServiceInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<TView>().To<TMediator>();
        }

        // Setup is the phase that may reach another module, and it runs once every Root has bound -
        // so the service's commands are in place whichever Root started first.
        public override void Setup()
        {
            base.Setup();

            if (_internalSignals == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"{GetType().Name} could not reach the screen service. Is ScreenServiceRoot in the scene?");
                return;
            }

            _internalSignals.RegisterScreen.Dispatch(new ScreenEntry
            {
                ViewType = typeof(TView),
                Screen = Screen,
                Owner = this
            });
        }

        public override void DestroyContext()
        {
            _internalSignals?.UnRegisterScreen.Dispatch(typeof(TView));

            base.DestroyContext();
        }
    }
}
