using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Models;
using Modules.DeviceDebuggerModule.RootsContexts;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Runs from Setup. In a development build or the Editor the panel child under the Root is
    /// switched on - it is authored inactive, so its view registers only now, after every Root
    /// has bound - and the Info rows are read. In a release build the panel is destroyed and the
    /// Root stays with a service that answers "not available".
    /// </summary>
    internal class InitializeDeviceDebuggerCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [Inject] private IFunctionProvider _functionProvider { get; set; }

        [Inject(nameof(DeviceDebuggerServiceContext))]
        private GameObject _root { get; set; }

        public override void Execute()
        {
            Transform panel = _root != null ? _root.transform.Find(DeviceDebuggerConstants.PANEL_OBJECT_NAME) : null;

            if (!DeviceDebuggerConstants.IS_AVAILABLE)
            {
                if (panel != null) Object.Destroy(panel.gameObject);
                return;
            }

            if (panel == null)
            {
                FlowLogger.LogError("DeviceDebuggerServiceRoot has no child named " + DeviceDebuggerConstants.PANEL_OBJECT_NAME
                                    + ", so the panel cannot open. Use the shipped prefab.");
                return;
            }

            _model.SetInfo(_functionProvider.Call<ReadDeviceInfoFunction>().ExecuteAndGetResult<List<InfoRowVO>>());
            panel.gameObject.SetActive(true);
        }
    }
}
