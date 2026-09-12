using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LoadingModule.Services;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// The step a chain binds to begin a set: <c>.ToSequence&lt;BeginLoadingCommand&gt;("Boot")</c>.
    /// Synchronous - the screen opens on its own signal, and the chain carries on to the steps it
    /// will report.
    /// </summary>
    public class BeginLoadingCommand : Command<string>
    {
        [Inject] private ILoadingService _loadingService { get; set; }

        public override void Execute(string set) => _loadingService.Begin(set);
    }
}
