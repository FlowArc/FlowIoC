using FlowIoC.BaseModule.Contexts;
using Modules.WorldPointerModule.Services;

namespace Modules.WorldPointerModule.RootsContexts
{
    /// <summary>
    /// One binding. The service subscribes its own LateUpdate in PostConstruct, registering a
    /// pointer is never a step a sequence waits on, and nothing outside the module needs telling
    /// - so there are no Commands, no signals, no Setup and no Launch.
    /// </summary>
    public class WorldPointerServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IWorldPointerService, WorldPointerService>();
        }
    }
}
