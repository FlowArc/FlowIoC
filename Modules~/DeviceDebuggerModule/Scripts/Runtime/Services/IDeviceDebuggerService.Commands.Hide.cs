using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.DeviceDebuggerModule.Services
{
    public partial interface IDeviceDebuggerService
    {
        public static partial class Commands
        {
            /// <summary>Closes the panel: <c>.ToSequence&lt;IDeviceDebuggerService.Commands.Hide&gt;()</c>.</summary>
            public class Hide : Command
            {
                [Inject] private IDeviceDebuggerService _debugger { get; set; }

                public override void Execute() => _debugger.Hide();
            }
        }
    }
}
