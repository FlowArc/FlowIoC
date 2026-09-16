#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Signals;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    /// <summary>Announces the toggle back, so the panel's God mode row shows what the module holds.</summary>
    public class EchoGodModeCommand : Command
    {
        [InjectSignal] private DeviceDebuggerTestSignals _signals { get; set; }

        [SignalParam] private bool _on { get; set; }

        public override void Execute()
        {
            FlowLogger.Log("God mode " + (_on ? "on" : "off") + ".");
            _signals.Outgoing.GodModeChanged.Dispatch(_on);
        }
    }
}

#endif
