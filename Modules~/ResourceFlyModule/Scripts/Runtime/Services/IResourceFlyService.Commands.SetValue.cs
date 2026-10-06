using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.ResourceFlyModule.Services
{
    public partial interface IResourceFlyService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Takes the saved value the signal carries for the key bound with the step:
            /// <c>.ToSequence&lt;IResourceFlyService.Commands.SetValue&gt;("Coin")</c> on a Signal&lt;int&gt;.
            /// </summary>
            public class SetValue : Command<string>
            {
                [SignalParam] private int _saved { get; set; }

                [Inject] private IResourceFlyService _service { get; set; }

                public override void Execute(string key) => _service.SetValue(key, _saved);
            }
        }
    }
}
