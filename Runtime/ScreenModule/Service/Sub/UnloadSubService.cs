using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    public class UnloadSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private DisposeSubService _dispose { get; set; }
        [Inject] private HideSubService _hide { get; set; }

        public void AllScreens(bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Close.AllScreens][isForce({isForce})]");

            foreach (var screenBody in _configModel.GetAllLoadedScreens())
            {
                Screen(screenBody, isForce);
            }
        }

        public void ScreensByManager(int managerId, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Close.AllScreensAtManager][isForce({isForce}) manager({managerId})]");

            foreach (var screenBody in _configModel.GetAllScreensAtManager(managerId))
            {
                Screen(screenBody, isForce);
            }
        }

        public void ScreensByTag(ScreenTag tag, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Close.AllScreensAtManager][isForce({isForce}) tag:({tag.ToString()})]");

            List<CD_Screen> tagConfigs = _configModel.GetTagConfigs(tag);

            foreach (CD_Screen config in tagConfigs)
            {
                if (_configModel.IsScreenLoaded(config, out var screenBody))
                {
                    Screen(screenBody, isForce);
                }
            }
        }

        public void Screen<T>(int managerId = 0, bool isForce = false) where T : IScreenBody
        {
            var config = _configModel.GetScreenConfig(managerId, typeof(T));

            if (_configModel.IsScreenLoaded(config, out var screenBody))
            {
                Screen(screenBody, isForce);
            }
            else
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenService.Hide] Layer({screenBody.Data.LayerIndex}) is empty at manager({managerId})!");
        }

        public void Screen(IScreenBody screenBody, bool isForce = false)
        {
            screenBody.Data.AddState(ScreenState.Unloading);
            _hide.Screen(screenBody, isForce);
        }

        internal void AfterHide(IScreenBody screenBody)
        {
            screenBody.Data.RemoveState(ScreenState.Unloading);
            RemoveFromPools(screenBody);
            _dispose.Screen(screenBody);
        }

        private void RemoveFromPools(IScreenBody screenBody)
        {
            _runtimeModel.RemoveFromActivePools(screenBody);
            _runtimeModel.RemoveFromPassivePool(screenBody);
        }
    }
}