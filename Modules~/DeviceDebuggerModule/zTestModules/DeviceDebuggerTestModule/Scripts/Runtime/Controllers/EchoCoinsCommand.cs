#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Signals;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers
{
    /// <summary>The slider and the +1000 button both land here; the value goes back out as Coins now.</summary>
    public class EchoCoinsCommand : Command
    {
        [InjectSignal] private DeviceDebuggerTestSignals _signals { get; set; }

        [SignalParam] private int _coins { get; set; }

        public override void Execute()
        {
            FlowLogger.Log("Coins: " + _coins + ".");
            _signals.Outgoing.CoinsChanged.Dispatch(_coins);
        }
    }
}

#endif
