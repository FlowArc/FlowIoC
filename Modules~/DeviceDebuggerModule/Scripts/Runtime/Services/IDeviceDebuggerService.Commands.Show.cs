using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Services
{
    public partial interface IDeviceDebuggerService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Opens the panel on the tab given where the step is bound:
            /// <c>.ToSequence&lt;IDeviceDebuggerService.Commands.Show&gt;(DebugTab.Console)</c>, or
            /// <c>DebugTab.Last</c> for whichever tab was open. A settings screen's hidden button
            /// binds this rather than writing a Command.
            /// </summary>
            public class Show : Command<DebugTab>
            {
                [Inject] private IDeviceDebuggerService _debugger { get; set; }

                public override void Execute(DebugTab tab) => _debugger.Show(tab);
            }
        }
    }
}
