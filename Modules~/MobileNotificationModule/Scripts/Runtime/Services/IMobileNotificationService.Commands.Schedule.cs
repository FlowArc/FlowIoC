using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.MobileNotificationModule.Services
{
    public partial interface IMobileNotificationService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Schedules the template named where the step is bound, after the template's own
            /// DefaultAfterMinutes: <c>.ToSequence&lt;IMobileNotificationService.Commands.Schedule&gt;("FreeChest")</c>.
            /// A notification whose time is computed is a game Command injecting the service
            /// and calling Schedule with the time.
            /// </summary>
            public class Schedule : Command<string>
            {
                [Inject] private IMobileNotificationService _notifications { get; set; }

                public override void Execute(string key) => _notifications.Schedule(key);
            }
        }
    }
}
