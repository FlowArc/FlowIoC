#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    /// <summary>Two hundred rows at once, to see the list virtualise and the ring roll.</summary>
    public class SpamTestLogsCommand : Command
    {
        public override void Execute()
        {
            for (int i = 1; i <= 200; i++)
                FlowLogger.Log("Spam line " + i + " of 200.");
        }
    }
}

#endif
