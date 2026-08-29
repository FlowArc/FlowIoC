#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Services;
using Modules.CountdownServiceModule.CountdownServiceTestModule.Models;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Controllers
{
    /// <summary>
    /// Ends the test countdown early. The service runs the stop callbacks the start command
    /// registered, so the Stopped signal reaches the view through the same path as every tick.
    /// </summary>
    internal class StopTestCountdownCommand : Command
    {
        [Inject] private ICountdownService _countdownService { get; set; }
        [Inject] private ICountdownTestModel _testModel { get; set; }

        public override void Execute()
        {
            _countdownService.Stop(_testModel.CountdownId);
        }
    }
}
#endif
