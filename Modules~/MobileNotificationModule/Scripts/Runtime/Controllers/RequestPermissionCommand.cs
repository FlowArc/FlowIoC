using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Enums;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>
    /// Asks the OS unless it already said yes, holding the sequence until it answers. The three
    /// ways out: the OS answered; the gateway threw, in which case the current status is the
    /// answer; the Editor gateway, which answers on the same frame. A gateway answers last, so
    /// the throw path cannot follow an answer.
    /// </summary>
    internal class RequestPermissionCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        [SignalParam] private Action<NotificationPermission> _answered { get; set; }

        public override void Execute()
        {
            if (_model.Permission == NotificationPermission.Granted)
            {
                _answered?.Invoke(NotificationPermission.Granted);
                return;
            }

            Retain();

            try
            {
                _gateway.RequestPermission(Answer);
            }
            catch (Exception e)
            {
                FlowLogger.LogError("RequestPermission - the platform refused: " + e.Message);
                Answer(_model.Permission);
            }
        }

        private void Answer(NotificationPermission permission)
        {
            _model.SetPermission(permission);
            FlowLogger.Log("RequestPermission - " + permission);
            _answered?.Invoke(permission);
            Release();
        }
    }
}
