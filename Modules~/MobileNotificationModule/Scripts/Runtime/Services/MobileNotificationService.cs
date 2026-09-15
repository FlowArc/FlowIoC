using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Signals;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through the internal signals, so each one is a
    /// step the Flow Console shows and the decisions - unknown template, permission denied - are
    /// taken in a Command. The reads come straight from the model.
    /// </summary>
    public class MobileNotificationService : IMobileNotificationService
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [InjectSignal] private MobileNotificationInternalSignals _signals { get; set; }

        public NotificationPermission Permission => _model.Permission;

        public string OpenedFromKey => _model.OpenedFromKey;

        public string OpenedFromTag => _model.OpenedFromTag;

        public IReadOnlyList<NotificationDraftVO> Scheduled => _model.Scheduled;

        // A null payload never reaches a SignalParam: the caller that wants no answer gets a no-op.
        public void RequestPermission(Action<NotificationPermission> answered = null) =>
            _signals.RequestPermission.Dispatch(answered ?? (_ => { }));

        public void Schedule(string key, string tag = null, params object[] args) =>
            _signals.Schedule.Dispatch(new NotificationRequestVO {Key = key, Tag = tag, Args = args});

        public void Schedule(string key, TimeSpan after, string tag = null, params object[] args) =>
            _signals.Schedule.Dispatch(new NotificationRequestVO {Key = key, Tag = tag, After = after, Args = args});

        public void Schedule(string key, DateTime at, string tag = null, params object[] args) =>
            _signals.Schedule.Dispatch(new NotificationRequestVO {Key = key, Tag = tag, At = at, Args = args});

        public void Cancel(string key, string tag = null) =>
            _signals.Cancel.Dispatch(new NotificationRequestVO {Key = key, Tag = tag});

        public void CancelAll() => _signals.CancelAll.Dispatch();

        public void OpenSettings() => _signals.OpenSettings.Dispatch();
    }
}
