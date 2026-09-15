using System;
using FlowIoC.BaseModule.Signals;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;

namespace Modules.MobileNotificationModule.Signals
{
    /// <summary>
    /// What the service says to its own commands. Nothing here leaves the module: the module has
    /// no public holder at all, because a Service answers the caller it was given, and the two
    /// facts settled at boot are read from the interface rather than announced.
    /// </summary>
    internal class MobileNotificationInternalSignals : ISignalHolder
    {
        public Signal Initialize = new();
        public Signal<Action<NotificationPermission>> RequestPermission = new();
        public Signal<NotificationRequestVO> Schedule = new();
        public Signal<NotificationRequestVO> Cancel = new();
        public Signal CancelAll = new();
        public Signal Left = new();
        public Signal Returned = new();
        public Signal OpenSettings = new();
    }
}