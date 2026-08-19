using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Config
{
    internal class ScreenConfigModel : IScreenConfigModel
    {
        [ShowInModelViewer] private readonly Dictionary<int, ScreenManagerVO> _screenManagers = new();
        [ShowInModelViewer] private readonly Dictionary<int, IContext> _screenManagerContextMap = new();
        
        [ShowInModelViewer] private readonly Dictionary<ScreenConfig, IScreenBody> _configMap = new();
        [ShowInModelViewer] private readonly Dictionary<ScreenTag, List<ScreenConfig>> _tagConfigs = new();
        
        public void RegisterScreenManager(ScreenManagerVO manager, IContext registeredContext)
        {
            if (manager == null)
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenConfigModel][RegisterScreenManager] Screen manager is null!");

            if (registeredContext == null)
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenConfigModel][RegisterScreenManager] registeredContext is null!");

            if (manager.ScreenLayerList == null || manager.ScreenLayerList.Count == 0)
                FlowLogger.LogWarning(SystemLogType.Screen, $"[ScreenConfigModel] Manager {manager.ManagerID} has no layers!");

            if (_screenManagers.ContainsKey(manager.ManagerID))
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Manager has already been registered with ID: {manager.ManagerID}");
            }
            else
            {
                _screenManagers[manager.ManagerID] = manager;
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenConfigModel] Registered screen manager with ID: {manager.ManagerID}");
            }

            if (_screenManagerContextMap.ContainsKey(manager.ManagerID))
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Context has already been registered for manager with ID: {manager.ManagerID}");
            }
            else
            {
                _screenManagerContextMap[manager.ManagerID] = registeredContext;
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenConfigModel] Registered manager context with ID: {manager.ManagerID}");
            }
        }

        public IContext GetManagerRegisteredContext(int managerId) => _screenManagerContextMap[managerId];

        public void RegisterScreenConfig(int managerId, Type type, ScreenConfig config)
        {
            var manager = _screenManagers[managerId];
            if (manager == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenConfigModel] Can not find screen manager to add config!");
                return;
            }

            manager.ScreenConfigs ??= new Dictionary<Type, ScreenConfig>();

            if (manager.ScreenConfigs.TryAdd(type, config))
            {
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenConfigModel] Registered screen config for type: {type.Name}");
                AddScreenToAllConfigs(config);
                AddScreenToTagConfigs(config.Tag, config);
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenConfigModel] Screen config for type {type.Name} is already registered! Updating...");
                manager.ScreenConfigs[type] = config;
            }
        }

        private void AddScreenToAllConfigs(ScreenConfig config) => _configMap.TryAdd(config, null);

        private void AddScreenToTagConfigs(ScreenTag screenTag, ScreenConfig config)
        {
            if (!_tagConfigs.ContainsKey(screenTag))
                _tagConfigs.Add(screenTag, new List<ScreenConfig>());

            _tagConfigs[screenTag].Add(config);
        }

        public void UnRegisterScreenConfig(int managerId, Type type, ScreenConfig config)
        {
            var manager = _screenManagers[managerId];
            if (manager == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenConfigModel] Can not find screen manager to remove config!");
                return;
            }

            if (manager.ScreenConfigs.ContainsKey(type))
            {
                manager.ScreenConfigs.Remove(type);
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenConfigModel] Removed screen config for type: {type.Name}");
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenConfigModel] Screen config for type {type.Name} not found!");
            }
        }

        public ScreenManagerVO GetScreenManager(int managerId)
        {
            if (_screenManagers.TryGetValue(managerId, out var manager)) return manager;

            FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Screen manager not found for ID: {managerId}");
            return null;
        }
        
        public List<ScreenConfig> GetAllRegisteredConfigs()
        {
            return _configMap.Keys.ToList();
        }

        public List<ScreenConfig> GetManagerConfigs(int managerId)
        {
            var manager = GetScreenManager(managerId);
            var list = new List<ScreenConfig>(manager.ScreenConfigs.Values);
            return list;
        }
        public List<ScreenConfig> GetTagConfigs(ScreenTag tag) => _tagConfigs[tag];

        public ScreenConfig GetScreenConfig(int managerId, Type screenType)
        {
            if (screenType == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenConfigModel] Cannot get screen config: Screen type is null");
                return null;
            }

            if (!_screenManagers.TryGetValue(managerId, out var manager))
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Screen manager not found for ID: {managerId}");
                return null;
            }

            if (manager.ScreenConfigs == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Screen configs dictionary is null for manager: {managerId}");
                return null;
            }

            if (manager.ScreenConfigs.TryGetValue(screenType, out var config))
            {
                return config;
            }

            FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Screen config not found for type: {screenType.Name}");
            return null;
        }

        public void ConfigToScreen(ScreenConfig config, IScreenBody screen) => _configMap[config] = screen;

        public bool IsScreenLoaded(ScreenConfig config, out IScreenBody screen)
        {
            screen = _configMap[config];
            return _configMap[config] != null;
        }

        public List<IScreenBody> GetAllLoadedScreens()
        {
            List<IScreenBody> result = new();
            var keys = _configMap.Keys.ToList();

            foreach (var key in keys)
            {
                if (IsScreenLoaded(key, out var screen))
                    result.Add(screen);
            }
            return result;
        }

        public List<IScreenBody> GetAllScreensAtManager(int managerId)
        {
            List<IScreenBody> result = new();
            foreach (var screenConfig in _screenManagers[managerId].ScreenConfigs.Values)
            {
                if (IsScreenLoaded(screenConfig, out var screen))
                    result.Add(screen);
            }
            return result;
        }

        public void CopyDataFromConfig(ScreenVO screenData)
        {
            var config = GetScreenConfig(screenData.ManagerId, screenData.ScreenType);
            CopyDataFromConfig(screenData, config);
        }

        public void CopyDataFromConfig(ScreenVO screenData, ScreenConfig config)
        {
            if (config == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenConfigModel] Cannot copy data: config is null for type: {screenData?.ScreenType?.Name}");
                return;
            }

            screenData.LayerIndex = config.DefaultLayer;
            screenData.Tag = config.Tag;
            screenData.HasHideAnimation = config.HasHideAnimation;
            screenData.HasShowAnimation = config.HasShowAnimation;
            screenData.ForceOpenAtFullLayer = false;
            screenData.ForceOpenAtDuplication = false;
            screenData.AddToHistory = false;
            screenData.Parameters = null;
        }
    }
}