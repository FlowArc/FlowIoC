#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    public class LogTestLineCommand : Command
    {
        public override void Execute() => FlowLogger.Log("A line from the test scene at " + DateTime.Now.ToString("HH:mm:ss") + ".");
    }
}

#endif
