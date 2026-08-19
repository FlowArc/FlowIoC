using FlowIoC.BaseModule.Contexts;
using FlowIoC.PoolModule.Controllers;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;
using FlowIoC.PoolModule.Services;
using FlowIoC.PoolModule.Services.Sub;
using FlowIoC.PoolModule.Services.Sub.Load;
using FlowIoC.PoolModule.Signals;
using FlowIoC.PoolModule.ViewsMediators.ConfigAdapter;

namespace FlowIoC.PoolModule.RootsContexts
{
    public class PoolAdapterContext : Context
    {
        private PoolServiceSignals _poolServiceSignals;

        public override void InjectionBindings()
        {
            base.InjectionBindings();

        }

        public override void MediationBindings()
        {
            base.MediationBindings();
            MediationBinder.Bind<PoolConfigAdapterView>().To<PoolConfigAdapterMediator>();
        }

        public override void SignalBindings()
        {
            base.SignalBindings();
            //_poolServiceSignals = InjectionBinderCrossContext.Bind<PoolServiceSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
        }

        public override void Setup()
        {
            base.Setup();
        }
    }
}