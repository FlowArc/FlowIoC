using FlowIoC.BaseModule.Contexts;
using FlowIoC.PoolModule.Controllers;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;
using FlowIoC.PoolModule.Services;
using FlowIoC.PoolModule.Services.Sub;
using FlowIoC.PoolModule.Services.Sub.Getter;
using FlowIoC.PoolModule.Services.Sub.Load;
using FlowIoC.PoolModule.Signals;

namespace FlowIoC.PoolModule.RootsContexts
{
    public class PoolServiceContext : Context
    {
        private IPoolService _service;
        private PoolServiceSignals _poolServiceSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _poolServiceSignals = InjectionBinderCrossContext.Bind<PoolServiceSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            _service = InjectionBinderCrossContext.Bind<IPoolService, PoolService>();

            InjectionBinder.Bind<IPoolConfigModel, PoolConfigModel>();
            InjectionBinder.Bind<IPoolRuntimeModel, PoolRuntimeModel>();

            InjectionBinder.Bind<IPoolGetterSubService, PoolGetterSubService>();
            InjectionBinder.Bind<CreateSubService>();
            InjectionBinder.Bind<DestroySubService>();
            InjectionBinder.Bind<ReturnSubService>();
            InjectionBinder.Bind<CheckSubService>();
            InjectionBinder.Bind<LoadSubService>();
            InjectionBinder.Bind<AddressableLoadSubService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_poolServiceSignals.RegisterPoolConfigs).ToSequence<RegisterPoolConfigCommand>();
            CommandBinder.Bind(_poolServiceSignals.UnRegisterConfigs).ToSequence<UnregisterPoolConfigCommand>();
        }

        public override void Setup()
        {
            base.Setup();
            _service.AutoInitializeAll();
        }
    }
}