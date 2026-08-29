#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Controllers;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Models;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Signals;
using Modules.CountdownServiceModule.CountdownServiceTestModule.ViewsMediators;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.RootsContexts
{
    public class CountdownServiceTestContext : Context
    {
        private CountdownServiceTestSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<CountdownServiceTestSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<ICountdownTestModel, CountdownTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<CountdownTestView>().To<CountdownTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.StartTestCountdown)
                .ToSequence<StartTestCountdownCommand>();

            CommandBinder.Bind(_signals.Incoming.StopTestCountdown)
                .ToSequence<StopTestCountdownCommand>();
        }
    }
}
#endif
