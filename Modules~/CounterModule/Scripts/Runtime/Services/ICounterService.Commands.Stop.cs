using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.CounterModule.Services
{
    public partial interface ICounterService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Ends a counter now, the counter named where the step is bound:
            /// <c>.ToSequence&lt;ICounterService.Commands.Stop&gt;("MatchTimer")</c>. Its stop
            /// callbacks run, its complete callbacks do not.
            /// </summary>
            public class Stop : Command<string>
            {
                [Inject] private ICounterService _counterService { get; set; }

                public override void Execute(string id) => _counterService.Stop(id);
            }
        }
    }
}
