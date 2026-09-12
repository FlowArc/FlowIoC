using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Services;

namespace Modules.LoadingModule.Controllers
{
    /// <summary>
    /// The step a chain binds where it wants to wait for a set: <c>.ToSequence&lt;AwaitLoadingCommand&gt;("Boot")</c>.
    /// The chain's own steps are already waited on by the group; this is for the steps other modules
    /// report on their own, and it is what holds the chain behind a failed boot. Three ways out, and
    /// every one resolves the retain: completed releases, failed stops, and a throw stops.
    /// </summary>
    public class AwaitLoadingCommand : Command<string>
    {
        [Inject] private ILoadingService _loadingService { get; set; }

        public override async void Execute(string set)
        {
            Retain();

            try
            {
                bool completed = await _loadingService.Await(set);

                if (!completed)
                {
                    // The failure itself was logged where it happened; this line only says the chain stopped.
                    FlowLogger.Log(FlowLogType.LoadingModule, $"AwaitLoadingCommand - '{set}' did not complete, so the sequence stops here.");
                    Stop();
                    return;
                }

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.LoadingModule, $"AwaitLoadingCommand threw while waiting for '{set}': {exception}");
                Stop();
            }
        }
    }
}
