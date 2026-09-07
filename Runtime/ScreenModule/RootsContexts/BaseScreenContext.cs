using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.ViewsMediators.Manager;

namespace FlowIoC.ScreenModule.RootsContexts
{
    /// <summary>
    /// The context of the Root that owns a ScreenManager. It binds the manager's mediation and
    /// reaches the screen service in Setup, which is the phase allowed to reach another module;
    /// the service is bound by ScreenServiceRoot, which starts before this Root does.
    /// </summary>
    public class BaseScreenContext : Context
    {
        protected internal IScreenService _screenService;

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<ScreenManager>().To<ScreenManagerMediator>();
        }

        public override void Setup()
        {
            base.Setup();

            _screenService = InjectionBinderCrossContext.GetInstance<IScreenService>();
        }
    }
}
