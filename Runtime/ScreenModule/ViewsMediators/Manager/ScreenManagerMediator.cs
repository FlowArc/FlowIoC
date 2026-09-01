using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Signals;

namespace FlowIoC.ScreenModule.ViewsMediators.Manager
{
    internal class ScreenManagerMediator : IMediator
    {
        [Inject] private ScreenManager _view { get; set; }
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
            ScreenManagerVO manager = _view.ManagerData;

            FlowLogger.Log(SystemLogType.Screen, $"Registering screen manager with ID: {manager.ManagerID}");
            var viewInjector = _view.GetComponent<ViewInjector>();
            var context = viewInjector.GetContextOfView(_view);
            _screenServiceInternalSignals.RegisterManager.Dispatch(manager, context);

            List<CD_Screen> configs = _view.GetScreenConfigs();
            if (configs == null || configs.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Screen, $"Screen manager {manager.ManagerID} has no screen configs!");
            }
            else
            {
                FlowLogger.Log(SystemLogType.Screen, $"Registering {configs.Count} screen configs for manager {manager.ManagerID}");
                
                _screenServiceInternalSignals.RegisterConfigs.Dispatch(manager.ManagerID, configs);
            }
        }

        private void UnRegisterScreenConfig(List<CD_Screen> configs)
        {
            try
            {
                ScreenManagerVO manager = _view.ManagerData;
                if (manager == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"Cannot unregister screen manager: ScreenManagerVO is null for {_view.name}");
                    return;
                }

                FlowLogger.Log(SystemLogType.Screen, $"Unregistering screen manager with ID: {manager.ManagerID}");

                if (configs == null || configs.Count == 0)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen, "No screen configs!");
                }
                else
                {
                    FlowLogger.Log(SystemLogType.Screen, $"Unregistering {configs.Count} screen configs for manager {manager.ManagerID}");
                    _screenServiceInternalSignals.UnRegisterConfigs.Dispatch(manager.ManagerID, configs);
                }
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"Error in ScreenMediatorNew UnRegister: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}