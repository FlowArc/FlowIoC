#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Controller;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    /// <summary>What an exception looks like on the panel: the badge, the red row, the trace.</summary>
    public class ThrowTestExceptionCommand : Command
    {
        public override void Execute() =>
            throw new InvalidOperationException("Thrown on purpose from the Device Debugger test scene.");
    }
}

#endif
