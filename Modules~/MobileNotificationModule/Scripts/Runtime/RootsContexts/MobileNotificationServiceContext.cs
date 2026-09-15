using FlowIoC.BaseModule.Contexts;
using Modules.MobileNotificationModule.Controllers;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;
using Modules.MobileNotificationModule.Signals;

namespace Modules.MobileNotificationModule.RootsContexts
{
    /// <summary>
    /// Declares the module and nothing else. Setup readies the platform; the two flows the Root's
    /// pause and resume drive - the reminders out when the app is left, everything the absent
    /// player was promised taken back when they return - read below in the order they run.
    /// </summary>
    public class MobileNotificationServiceContext : Context
    {
        private MobileNotificationInternalSignals _signals;
        private bool _initialized;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<MobileNotificationInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            // The model before the service, so the catalogue is read before anything asks for it.
            InjectionBinder.Bind<IMobileNotificationModel, MobileNotificationModel>();

            // One platform, one gateway. The commands never learn which; the Editor gets the one
            // that logs, so a flow can be read in the Flow Console without a device.
#if UNITY_ANDROID && !UNITY_EDITOR
            InjectionBinder.Bind<INotificationGateway, AndroidNotificationGateway>();
#elif UNITY_IOS && !UNITY_EDITOR
            InjectionBinder.Bind<INotificationGateway, IosNotificationGateway>();
#else
            InjectionBinder.Bind<INotificationGateway, EditorNotificationGateway>();
#endif

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IMobileNotificationService, MobileNotificationService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Initialize)
                .ToSequence<InitializeMobileNotificationCommand>()
                .ToSequence<PreparePicturesCommand>()
                .ToSequence<ReadOpenedFromCommand>()
                .ToSequence<CancelReturnRemindersCommand>()
                .ToSequence<ClearDeliveredCommand>();

            CommandBinder.Bind(_signals.RequestPermission).ToSequence<RequestPermissionCommand>();

            CommandBinder.Bind(_signals.Schedule).ToSequence<ScheduleNotificationCommand>();

            CommandBinder.Bind(_signals.Cancel).ToSequence<CancelNotificationCommand>();

            CommandBinder.Bind(_signals.CancelAll).ToSequence<CancelAllNotificationsCommand>();

            CommandBinder.Bind(_signals.OpenSettings).ToSequence<OpenNotificationSettingsCommand>();

            CommandBinder.Bind(_signals.Left).ToSequence<ScheduleReturnRemindersCommand>();

            CommandBinder.Bind(_signals.Returned)
                .ToSequence<CancelReturnRemindersCommand>()
                .ToSequence<ClearDeliveredCommand>()
                .ToSequence<ReadOpenedFromCommand>();
        }

        public override void Setup()
        {
            base.Setup();

            // The module brings itself up: channels registered, permission read, the tray cleared.
            _signals.Initialize.Dispatch();
            _initialized = true;
        }

        // Unity's OnApplicationPause reaches these through the Root. Before Setup there is
        // nothing to take back and no platform to talk to, so a pause that lands early is skipped.
        public override void PauseContext()
        {
            base.PauseContext();

            if (_initialized)
                _signals.Left.Dispatch();
        }

        public override void ResumeContext()
        {
            base.ResumeContext();

            if (_initialized)
                _signals.Returned.Dispatch();
        }
    }
}