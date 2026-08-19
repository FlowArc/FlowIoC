using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    public class LoadSubService
    {
        [Inject] private IScreenConfigModel _configModel { get; set; }
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private DirectPrefabLoadSubService _directPrefabLoadSubService{ get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService{ get; set; }
        [Inject] private ResourceLoadSubService _resourceLoadSubService{ get; set; }

        public async void All(bool isTest = false, Action completeCallback = null, Action<int,int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenService.Load.All]");
            List<ScreenConfig> allConfigs = _configModel.GetAllRegisteredConfigs();
            await LoadByConfigList(allConfigs, isTest, completeCallback, loadingProgressCallback);
        }
        public async void ScreensAtManager(int managerId = 0, bool isTest = false, Action completeCallback = null, Action<int,int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenService.Load.ScreensAtManager]");
            List<ScreenConfig> allConfigs = _configModel.GetManagerConfigs(managerId);
            await LoadByConfigList(allConfigs, isTest, completeCallback, loadingProgressCallback);
        }
        public async void ByTag(ScreenTag tag, bool isTest = false, Action completeCallback = null, Action<int,int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.ByTag] {tag}");
            var tagConfigs = _configModel.GetTagConfigs(tag);
            await LoadByConfigList(tagConfigs, isTest, completeCallback, loadingProgressCallback);
        }
        private async Task LoadByConfigList(List<ScreenConfig> configs, bool isTest, 
            Action completeCallback, Action<int,int> loadingProgressCallback)
        {
            for (var index = 0; index < configs.Count; index++)
            {
                var config = configs[index];
                if (_configModel.IsScreenLoaded(config, out _))
                {
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load][LoadByConfigList] Screen is already loaded: {config.AddressableKey}");
                    loadingProgressCallback?.Invoke(index,configs.Count);
                    continue;
                }
                var screen = await LoadScreenByConfig(config);
                if (screen == null) continue;
                screen.Data.ScreenType = screen.GetType();
                _configModel.CopyDataFromConfig(screen.Data, config);
                _runtimeModel.AddToPassivePool(screen);
                loadingProgressCallback?.Invoke(index,configs.Count);
            }

            completeCallback?.Invoke();
        }
        
        internal async Task<IScreenBody> Screen(ScreenVO screenData)
        {
            var config = _configModel.GetScreenConfig(screenData.ManagerId, screenData.ScreenType);
            return await LoadScreenByType(config);
        }
        private async Task<IScreenBody> LoadScreenByConfig(ScreenConfig config)
        {
            return await LoadScreenByType(config);
        }
        private async Task<IScreenBody> LoadScreenByType(ScreenConfig config)
        {
            switch (config.LoadType)
            {
                case ScreenLoadType.Addressable:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Attempting Addressable load for {config.AddressableKey}");
                    return await _addressableLoadService.LoadScreen(config);

                case ScreenLoadType.Resource:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Attempting Resource load for {config.AddressableKey}");
                    return await _resourceLoadSubService.LoadScreen(config);

                case ScreenLoadType.DirectPrefab:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Attempting DirectPrefab load for {config.AddressableKey}");
                    return await _directPrefabLoadSubService.LoadScreen(config);

                default:
                    FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService.Load][LoadScreenByType] Unknown load type {config.LoadType} for screen {config.AddressableKey}");
                    return default;
            }
        }
    }
}