using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.MobileNotificationModule.Services
{
    public partial interface IMobileNotificationService
    {
        public static partial class Commands
        {
            /// <summary>Takes back everything scheduled: <c>.ToSequence&lt;IMobileNotificationService.Commands.CancelAll&gt;()</c>.</summary>
            [DebugOption("Mobile Notification", "Cancel all")]
            public class CancelAll : Command
            {
                [Inject] private IMobileNotificationService _notifications { get; set; }

                public override void Execute() => _notifications.CancelAll();
            }
        }
    }
}
