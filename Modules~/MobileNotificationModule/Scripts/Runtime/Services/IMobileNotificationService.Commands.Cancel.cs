using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.MobileNotificationModule.Services
{
    public partial interface IMobileNotificationService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Takes back the untagged notification of the template named where the step is bound:
            /// <c>.ToSequence&lt;IMobileNotificationService.Commands.Cancel&gt;("FreeChest")</c>.
            /// A tagged one is cancelled from a game Command with the tag.
            /// </summary>
            public class Cancel : Command<string>
            {
                [Inject] private IMobileNotificationService _notifications { get; set; }

                public override void Execute(string key) => _notifications.Cancel(key);
            }
        }
    }
}
