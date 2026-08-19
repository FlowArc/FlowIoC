using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    public class HideSubService
    {
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private UnloadSubService _unload { get; set; }

        public void AllScreens(bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Hide.AllScreens][isForce({isForce})]");

            foreach (var screenBody in _runtimeModel.GetAllActiveScreens())
            {
                Screen(screenBody, isForce);
            }
        }
        public void ScreensAtManager(int managerId, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Hide.AllScreensAtManager][isForce({isForce})] manager({managerId})!");

            if (!_runtimeModel.GetActiveManagerScreens(managerId, out List<IScreenBody> list)) return;
            foreach (IScreenBody screenBody in list)
            {
                Screen(screenBody, isForce);
            }
        }
        public void ScreenInLayer(int layerIndex, int managerId = 0, bool isForce = false)
        {
            if (_runtimeModel.IsLayerFull(layerIndex, managerId, out IScreenBody screenBody))
            {
                FlowLogger.Log(SystemLogType.Screen,
                $"[ScreenService.Hide][isForce({isForce})] Layer({layerIndex}) at manager({managerId})!");

                Screen(screenBody, isForce);
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenService.Hide][isForce({isForce})] Layer({layerIndex}) is empty at manager({managerId})!");
            }
        }
        public void ScreensByTag(ScreenTag tag, int managerId = 0, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Hide.ScreensByTag][isForce({isForce})] tag:{tag.ToString()}");

            if (!_runtimeModel.GetActiveTagScreens(tag, managerId, out var tagScreens))
                return;

            foreach (IScreenBody screenBody in tagScreens)
            {
                Screen(screenBody, isForce);
            }
        }
        public void Screen<T>(int managerId = 0, bool isForce = false) where T : IScreenBody
        {
            if (_runtimeModel.IsScreenActive(typeof(T), managerId, out IScreenBody screenBody))
            {
                Screen(screenBody, isForce);
            }
            else
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenService.Hide][isForce({isForce})] Screen ({typeof(T).Name}) is not active at manager({managerId})!");
        }
        public void Screen(IScreenBody screenBody, bool isForce = false)
        {
            if (screenBody == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenService.Hide.Screen] Screenbody is null");
                return;
            }

            if (screenBody.Data.HasState(ScreenState.InUse))
            {
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Hide.Screen] {screenBody.Data.ScreenType.Name}");
                if (isForce)
                {
                    HideAnimationCompleted(screenBody);
                }
                else
                {
                    screenBody.Hide();
                }
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Screen, $"[ScreenService.Hide.Screen] Cant close {screenBody.Data.ScreenType.Name}, state: {screenBody.Data.State}");
            }
        }
        private void HideAnimationCompleted(IScreenBody screenBody)
        {
            screenBody.HideCompleted -= HideAnimationCompleted;
            
            screenBody.Data.RemoveState(ScreenState.InHideAnimation);
            _runtimeModel.RemoveFromActivePools(screenBody);
            _runtimeModel.AddToPassivePool(screenBody);
            screenBody.ScreenHidden();

            if (screenBody.Data.HasState(ScreenState.Unloading))
                _unload.AfterHide(screenBody);
        }
        internal void Setup<T>(T screenBody) where T : IScreenBody
        {
            screenBody.HideCompleted += HideAnimationCompleted;
        }
    }
}