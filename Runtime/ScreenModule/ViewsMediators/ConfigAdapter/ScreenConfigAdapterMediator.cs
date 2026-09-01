using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Signals;

namespace FlowIoC.ScreenModule.ViewsMediators.ConfigAdapter
{
    internal class ScreenConfigAdapterMediator : IMediator
    {
        [Inject] private ScreenConfigAdapterView _view { get; set; }
        [InjectSignal] private ScreenServiceInternalSignals _screenServiceInternalSignals { get; set; }

        public void OnRegister()
        {
            _view.UnRegisterScreenConfig += UnRegisterScreenConfig;
            Setup();
        }

        public void OnRemove()
        {
            _view.UnRegisterScreenConfig -= UnRegisterScreenConfig;
        }

        private void Setup()
        {
            var configs = _view.GetScreenConfigs();
            if (configs == null || configs.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Screen, $"ScreenConfigAdapter has no screen configs!");
            }
            else
            {
                FlowLogger.Log(SystemLogType.Screen,
                    $"Registering {configs.Count} screen configs for manager(id:{_view.ManagerIdToRegister}) with ScreenConfigAdapter");
                _screenServiceInternalSignals.RegisterConfigs.Dispatch(_view.ManagerIdToRegister, configs);
            }
        }

        private void UnRegisterScreenConfig(int managerID, List<CD_Screen> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Screen, "No screen configs!");
            }
            else
            {
                FlowLogger.Log(SystemLogType.Screen, $"Unregistering {configs.Count} screen configs for manager {managerID}");
                _screenServiceInternalSignals.UnRegisterConfigs.Dispatch(managerID, configs);
            }
        }
    }
}