using FlowIoC.BaseModule.Contexts;
using Modules.CountdownServiceModule.Controllers;
using Modules.CountdownServiceModule.Models;
using Modules.CountdownServiceModule.Services;
using Modules.CountdownServiceModule.Signals;

namespace Modules.CountdownServiceModule.RootsContexts
{
    public class CountdownServiceContext : Context
    {
        private CountdownServiceSignals _signals;
        private CountdownServiceSignalsInternal _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<CountdownServiceSignals>();
            _internalSignals = InjectionBinderCrossContext.Bind<CountdownServiceSignalsInternal>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<ICountdownModel, CountdownModel>();

            // The device clock is what makes the module work the moment it is installed. A game
            // that needs a clock the player cannot move writes its own ITimeSource and names it
            // here instead - this module lives in the game's own Assets, so this line is the
            // game's to change.
            InjectionBinder.Bind<ITimeSource, DeviceTimeSource>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<ICountdownService, CountdownService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            // Preparing the time source is what starts the clock, so the tick is only entered
            // once initialization has actually succeeded.
            CommandBinder.Bind(_signals.Incoming.Initialize)
                .ToSequence<InitializeCountdownServiceCommand>()
                .ToGroupAsParallel(_internalSignals.Tick);

            // One second: move the clock, then report it to every countdown, then queue the next
            // second. The loop is the tick re-entering itself.
            CommandBinder.Bind(_internalSignals.Tick)
                .ToSequence<TimeTickCommand>()
                .ToSequence<TickProcessAllDataCommand>()
                .ToGroupAsParallel(_internalSignals.Tick);

            // Starting a countdown is adding the entry, subscribing the caller to it, and giving
            // that caller its first value without waiting a second for it.
            CommandBinder.Bind(_internalSignals.AddCountdownData)
                .ToSequence<AddCountdownDataCommand>()
                .ToSequence<AddCallbacksCommand>()
                .ToSequence<TickProcessForNewDataCommand>();

            // Joining a countdown already running is the same two steps without the first.
            CommandBinder.Bind(_internalSignals.AddCallbacks)
                .ToSequence<AddCallbacksCommand>()
                .ToSequence<TickProcessForNewDataCommand>();

            CommandBinder.Bind(_internalSignals.RemoveCallbacks)
                .ToSequence<RemoveCallbacksCommand>();

            CommandBinder.Bind(_internalSignals.StopCountdown)
                .ToSequence<StopCountdownCommand>();
        }

        public override void Setup()
        {
            base.Setup();

            // The module brings itself up. A game only dispatches Initialize again to retry a
            // time source that failed the first time.
            _signals.Incoming.Initialize.Dispatch();
        }
    }
}
