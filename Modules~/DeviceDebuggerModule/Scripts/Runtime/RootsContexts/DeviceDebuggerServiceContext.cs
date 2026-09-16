using FlowIoC.BaseModule.Contexts;
using Modules.DeviceDebuggerModule.Controllers;
using Modules.DeviceDebuggerModule.Models;
using Modules.DeviceDebuggerModule.Services;
using Modules.DeviceDebuggerModule.Signals;
using Modules.DeviceDebuggerModule.ViewsMediators;

namespace Modules.DeviceDebuggerModule.RootsContexts
{
    public class DeviceDebuggerServiceContext : Context
    {
        private DeviceDebuggerInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _internalSignals = InjectionBinder.Bind<DeviceDebuggerInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IDeviceDebuggerModel, DeviceDebuggerModel>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IDeviceDebuggerService, DeviceDebuggerService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<DeviceDebuggerView>().To<DeviceDebuggerMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.StartCapture).ToSequence<StartLogCaptureCommand>();

            CommandBinder.Bind(_internalSignals.StopCapture).ToSequence<StopLogCaptureCommand>();

            CommandBinder.Bind(_internalSignals.Initialize)
                .ToSequence<InitializeDeviceDebuggerCommand>()
                .ToSequence<ReportPanelStateCommand>();

            CommandBinder.Bind(_internalSignals.RequestPanelState).ToSequence<ReportPanelStateCommand>();

            CommandBinder.Bind(_internalSignals.Show)
                .ToSequence<DiscoverOptionsCommand>()
                .ToSequence<ShowPanelCommand>()
                .ToSequence<ReportPanelStateCommand>();

            CommandBinder.Bind(_internalSignals.Hide)
                .ToSequence<HidePanelCommand>()
                .ToSequence<ReportPanelStateCommand>();

            CommandBinder.Bind(_internalSignals.Toggle).ToSequence<TogglePanelCommand>();

            CommandBinder.Bind(_internalSignals.ActivateOption).ToSequence<ActivateOptionCommand>();

            CommandBinder.Bind(_internalSignals.FireSignal).ToSequence<FireSignalCommand>();

            CommandBinder.Bind(_internalSignals.ClearLogs).ToSequence<ClearLogsCommand>();

            CommandBinder.Bind(_internalSignals.CopyLogs).ToSequence<CopyLogsCommand>();

            CommandBinder.Bind(_internalSignals.CopyLog).ToSequence<CopyLogCommand>();

            CommandBinder.Bind(_internalSignals.CopyInfo).ToSequence<CopyInfoCommand>();
        }

        public override void Setup()
        {
            base.Setup();

            // The panel comes up once every Root has bound; the log capture started earlier, from
            // the service's PostConstruct, so the binding pass after this Root is in the ring.
            _internalSignals.Initialize.Dispatch();
        }

        public override void DestroyContext()
        {
            // Before the binder goes: the hook on FlowLogger is static and would outlive the model.
            _internalSignals.StopCapture.Dispatch();

            base.DestroyContext();
        }
    }
}
