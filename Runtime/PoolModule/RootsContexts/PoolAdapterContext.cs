using FlowIoC.BaseModule.Contexts;
using FlowIoC.PoolModule.ViewsMediators.ConfigAdapter;

namespace FlowIoC.PoolModule.RootsContexts
{
    public class PoolAdapterContext : Context
    {
        public override void MediationBindings()
        {
            base.MediationBindings();
            MediationBinder.Bind<PoolConfigAdapterView>().To<PoolConfigAdapterMediator>();
        }
    }
}
