using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.ResourceFlyModule.Services
{
    public partial interface IResourceFlyService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Reserves the amount the signal carries under the key bound with the step, ahead of the
            /// save that grants it: <c>.ToSequence&lt;IResourceFlyService.Commands.Reserve&gt;("Coin")</c>
            /// on a Signal&lt;int&gt;.
            /// </summary>
            public class Reserve : Command<string>
            {
                [SignalParam] private int _amount { get; set; }

                [Inject] private IResourceFlyService _service { get; set; }

                public override void Execute(string key) => _service.Reserve(key, _amount);
            }
        }
    }
}
