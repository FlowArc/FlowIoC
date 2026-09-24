using FlowIoC.BaseModule.Contexts;
using Modules.WorldPointerModule.Models;
using Modules.WorldPointerModule.Services;

namespace Modules.WorldPointerModule.RootsContexts
{
    /// <summary>
    /// Two bindings. The service subscribes its own LateUpdate in PostConstruct, registering is
    /// never a step a sequence waits on, and nothing outside the module needs telling - so there
    /// are no Commands, no signals, no Setup and no Launch.
    /// </summary>
    public class WorldPointerServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();

            // The registry stays inside the module; only the service crosses.
            InjectionBinder.Bind<IWorldPointerModel, WorldPointerModel>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IWorldPointerService, WorldPointerService>();
        }
    }
}