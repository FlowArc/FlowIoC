using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Models;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Every row in the ring as text on the clipboard, whatever the Console's filters show - the
    /// button says "Copy all logs", and the detail's own Copy is the one for a single row. The
    /// clipboard is the one export a phone has without a cable.
    /// </summary>
    internal class CopyLogsCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [Inject] private IFunctionProvider _functionProvider { get; set; }

        public override void Execute()
        {
            var rows = new List<ConsoleLog>();
            _model.Logs.CopyTo(rows, null);

            GUIUtility.systemCopyBuffer = _functionProvider.Call<FormatLogRowsFunction>()
                .AddParams(rows)
                .ExecuteAndGetResult<string>();
        }
    }
}
