using FlowIoC.BaseModule.Contexts;
using Modules.LoadingModule.Controllers;
using Modules.LoadingModule.Models;
using Modules.LoadingModule.Services;
using Modules.LoadingModule.Signals;

namespace Modules.LoadingModule.RootsContexts
{
    public class LoadingServiceContext : Context
    {
        private LoadingSignals _signals;
        private LoadingInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<LoadingInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<LoadingSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<ILoadingModel, LoadingModel>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<ILoadingService, LoadingService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            // Two ways in to the same run: the service, and a Connector.
            CommandBinder.Bind(_signals.Incoming.Begin).ToSequence<BeginSetCommand>();
            CommandBinder.Bind(_internalSignals.BeginSet).ToSequence<BeginSetCommand>();

            CommandBinder.Bind(_internalSignals.StepReported).ToSequence<ApplyStepReportCommand>();

            // Every change to a set: publish the snapshot, then see whether the set has ended.
            CommandBinder.Bind(_internalSignals.SetTouched)
                .ToSequence<PublishSetStatusCommand>()
                .ToSequence<CheckSetCompletionCommand>();

            CommandBinder.Bind(_internalSignals.WatchSet).ToSequence<WatchStallCommand>();
        }
    }
}