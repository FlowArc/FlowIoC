#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers;
using Modules.MobileNotificationModule.MobileNotificationTestModule.Signals;
using Modules.MobileNotificationModule.MobileNotificationTestModule.ViewsMediators;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.RootsContexts
{
    /// <summary>
    /// Every button is a flow that ends by re-reading the state, so the label always shows what
    /// the service knows. The permission ask is the shipped step, bound the way a game binds it.
    /// </summary>
    public class MobileNotificationTestContext : Context
    {
        private MobileNotificationTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<MobileNotificationTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<MobileNotificationTestView>().To<MobileNotificationTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.RequestPermission)
                .ToSequence<IMobileNotificationService.Commands.RequestPermission>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.ScheduleTest)
                .ToSequence<ScheduleTestNotificationCommand>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.CancelTest)
                .ToSequence<CancelTestNotificationCommand>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.CancelAll)
                .ToSequence<IMobileNotificationService.Commands.CancelAll>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.OpenSettings).ToSequence<OpenTestSettingsCommand>();

            CommandBinder.Bind(_signals.Leave)
                .ToSequence<SimulateLeaveCommand>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.Return)
                .ToSequence<SimulateReturnCommand>()
                .ToSequence<ReportNotificationStateCommand>();

            CommandBinder.Bind(_signals.ReportState).ToSequence<ReportNotificationStateCommand>();
        }

        // The view may register before this Root's Setup or after its Launch, depending on the
        // frame the injector lands in, so the state is reported from both places: here, once the
        // service has initialised, and from the mediator when the view registers.
        public override void Launch()
        {
            base.Launch();

            _signals.ReportState.Dispatch();
        }
    }
}

#endif