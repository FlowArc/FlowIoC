using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.ViewsMediators.Manager;

namespace FlowIoC.ScreenModule.RootsContexts
{
    public class BaseScreenContext : Context
    {
        protected internal IScreenService _screenService;

        protected override void CoreBindings()
        {
            base.CoreBindings();

            _screenService = InjectionBinderCrossContext.GetInstance<IScreenService>();

            MediationBinder.Bind<ScreenManager>().To<ScreenManagerMediator>();
        }
    }
}