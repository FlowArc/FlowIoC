#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Services;
using Modules.CounterModule.CounterTestModule.Models;

namespace Modules.CounterModule.CounterTestModule.Controllers
{
    /// <summary>
    /// Ends the test counter early. The service runs the stop callbacks the start command
    /// registered, so the Stopped signal reaches the view through the same path as every tick.
    /// </summary>
    internal class StopTestCounterCommand : Command
    {
        [Inject] private ICounterService _counterService { get; set; }
        [Inject] private ICounterTestModel _testModel { get; set; }

        public override void Execute()
        {
            _counterService.Stop(_testModel.CounterId);
        }
    }
}
#endif
