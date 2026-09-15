using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.Models;
using Modules.MobileNotificationModule.Services;

namespace Modules.MobileNotificationModule.Controllers
{
    /// <summary>Readies the platform, registers the channels and reads the permission into the model. Runs from Setup.</summary>
    internal class InitializeMobileNotificationCommand : Command
    {
        [Inject] private IMobileNotificationModel _model { get; set; }
        [Inject] private INotificationGateway _gateway { get; set; }

        public override void Execute()
        {
            try
            {
                _gateway.Initialize(_model.Channels);
                _model.SetPermission(_gateway.ReadPermission());
            }
            catch (Exception e)
            {
                FlowLogger.LogError("Initialize - the platform refused: " + e.Message);
                return;
            }

            FlowLogger.Log($"Initialize - permission {_model.Permission}, {_model.Channels.Count} channel(s)");
        }
    }
}
