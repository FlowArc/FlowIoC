#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.CounterModule.CounterTestModule.Controllers;
using Modules.CounterModule.CounterTestModule.Models;
using Modules.CounterModule.CounterTestModule.Signals;
using Modules.CounterModule.CounterTestModule.ViewsMediators;

namespace Modules.CounterModule.CounterTestModule.RootsContexts
{
    public class CounterTestContext : Context
    {
        private CounterTestSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<CounterTestSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<ICounterTestModel, CounterTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<CounterTestView>().To<CounterTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.StartTestCounter)
                .ToSequence<StartTestCounterCommand>();

            CommandBinder.Bind(_signals.Incoming.StopTestCounter)
                .ToSequence<StopTestCounterCommand>();
        }
    }
}
#endif
