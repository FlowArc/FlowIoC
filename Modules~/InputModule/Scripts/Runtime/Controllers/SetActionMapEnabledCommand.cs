using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.InputModule.ViewsMediators;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Modules.InputModule.Controllers
{
    /// <summary>
    /// Turns one action map of the module's asset on or off. Silencing input is done here rather
    /// than by ignoring signals downstream, so nothing is dispatched in the first place.
    /// </summary>
    public class SetActionMapEnabledCommand : Command
    {
        [SignalParam] private string _mapName   { get; set; }
        [SignalParam] private bool   _isEnabled { get; set; }

        public override void Execute()
        {
            FlowLogger.Log(FlowLogType.InputModule,
                $"{nameof(Execute)} - {nameof(SetActionMapEnabledCommand)} ({_mapName}, {_isEnabled})");

            InputView view = Object.FindFirstObjectByType<InputView>();

            if (view == null || view.Actions == null)
            {
                FlowLogger.LogError(FlowLogType.InputModule,
                    "No InputView with an action asset is in the scene.");

                return;
            }

            InputActionMap map = view.Actions.FindActionMap(_mapName);

            if (map == null)
            {
                FlowLogger.LogError(FlowLogType.InputModule,
                    $"Action map '{_mapName}' is not in {view.Actions.name}.");

                return;
            }

            if (_isEnabled)
                map.Enable();
            else
                map.Disable();
        }
    }
}
