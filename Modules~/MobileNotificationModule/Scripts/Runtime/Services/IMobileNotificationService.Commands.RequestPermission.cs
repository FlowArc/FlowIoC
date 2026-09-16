using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.MobileNotificationModule.Services
{
    public partial interface IMobileNotificationService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Asks the OS for permission and holds the sequence until it answers, so the step
            /// after it - a screen that reads the answer, a schedule - runs with the answer known:
            /// <c>.ToSequence&lt;IMobileNotificationService.Commands.RequestPermission&gt;()</c>.
            /// Bind it where the ask belongs - after the first level, on a settings toggle - never
            /// at boot.
            /// </summary>
            [DebugOption("Mobile Notification", "Request permission")]
            public class RequestPermission : Command
            {
                [Inject] private IMobileNotificationService _notifications { get; set; }

                public override void Execute()
                {
                    Retain();
                    _notifications.RequestPermission(_ => Release());
                }
            }
        }
    }
}
