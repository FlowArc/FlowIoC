using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.LoadingModule.Services
{
    public partial interface ILoadingService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Begins a set: <c>.ToSequence&lt;ILoadingService.Commands.Begin&gt;("Boot")</c>.
            /// Synchronous - the screen opens on its own signal, and the chain carries on to the
            /// steps it will report.
            /// </summary>
            public class Begin : Command<string>
            {
                [Inject] private ILoadingService _loadingService { get; set; }

                public override void Execute(string set) => _loadingService.Begin(set);
            }
        }
    }
}
