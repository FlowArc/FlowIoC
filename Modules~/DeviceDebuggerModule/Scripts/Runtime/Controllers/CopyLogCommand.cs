using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>The one row open in the detail, on the clipboard.</summary>
    internal class CopyLogCommand : Command
    {
        [Inject] private IFunctionProvider _functionProvider { get; set; }

        [SignalParam] private ConsoleLog _row { get; set; }

        public override void Execute()
        {
            if (_row == null) return;

            GUIUtility.systemCopyBuffer = _functionProvider.Call<FormatLogDetailFunction>()
                .AddParams(_row)
                .ExecuteAndGetResult<string>();
        }
    }
}
