using FlowIoC.BaseModule.Contexts;
using Modules.ResourceFlyModule.Models;
using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.RootsContexts
{
    public class ResourceFlyServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<IResourceFlyModel, ResourceFlyModel>();
            InjectionBinderCrossContext.Bind<IResourceFlyService, ResourceFlyService>();
        }
    }
}
