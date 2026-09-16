#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Enums;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    public class LogSpeedCommand : Command
    {
        [SignalParam] private float _speed { get; set; }

        public override void Execute() => FlowLogger.Log("Speed set to " + _speed + ".");
    }

    public class LogNameCommand : Command
    {
        [SignalParam] private string _name { get; set; }

        public override void Execute() => FlowLogger.Log("Name set to '" + _name + "'.");
    }

    public class LogMoodCommand : Command
    {
        [SignalParam] private TestMood _mood { get; set; }

        public override void Execute() => FlowLogger.Log("Mood set to " + _mood + ".");
    }
}

#endif
