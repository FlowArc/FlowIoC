using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;

namespace Modules.MainModule.Controllers
{
    internal class LogStartupCommand : Command
    {
        public override void Execute()
        {
            FlowLogger.Log(FlowLogType.MainModule, "[LogStartupCommand] MainModule launched.");
        }
    }
}
