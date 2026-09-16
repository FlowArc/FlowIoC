using System.Text;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Models;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Controllers
{
    internal class CopyInfoCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }

        public override void Execute()
        {
            var text = new StringBuilder();

            foreach (InfoRowVO row in _model.Info)
                text.Append(row.Label).Append(": ").Append(row.Value).Append('\n');

            GUIUtility.systemCopyBuffer = text.ToString();
        }
    }
}
