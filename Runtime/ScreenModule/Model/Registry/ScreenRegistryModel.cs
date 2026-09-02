using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Registry
{
    /// <summary>
    /// The screens and managers the service knows about. The two are registered independently
    /// and in any order - a screen context registers in its Setup, a manager when its mediator
    /// registers, and which Root finishes first is not this model's business. They only meet at
    /// Open, where a missing manager is reported the same way a missing screen is.
    /// </summary>
    internal class ScreenRegistryModel : IScreenRegistryModel
    {
        [ShowInModelViewer] private readonly Dictionary<int, ScreenManagerVO> _managers = new();
        [ShowInModelViewer] private readonly Dictionary<(int managerId, Type viewType), ScreenEntry> _screens = new();

        public void RegisterScreenManager(ScreenManagerVO manager)
        {
            if (manager == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenRegistryModel] Screen manager is null!");
                return;
            }

            if (manager.ScreenLayerList == null || manager.ScreenLayerList.Count == 0)
                FlowLogger.LogWarning(SystemLogType.Screen, $"[ScreenRegistryModel] Manager {manager.ManagerID} has no layers!");

            if (_managers.ContainsKey(manager.ManagerID))
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenRegistryModel] Manager has already been registered with ID: {manager.ManagerID}");
                return;
            }

            _managers[manager.ManagerID] = manager;
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRegistryModel] Registered screen manager with ID: {manager.ManagerID}");
        }

        public ScreenManagerVO GetScreenManager(int managerId)
        {
            if (_managers.TryGetValue(managerId, out ScreenManagerVO manager))
                return manager;

            FlowLogger.LogError(SystemLogType.Screen, $"[ScreenRegistryModel] Screen manager not found for ID: {managerId}");
            return null;
        }

        public bool RegisterScreen(ScreenEntry entry)
        {
            if (entry == null || entry.ViewType == null || entry.Screen == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenRegistryModel] Cannot register a screen without a view type and a ScreenCVO.");
                return false;
            }

            if (!entry.Screen.Load.IsValid)
            {
                string owner = entry.Owner == null ? "A context" : entry.Owner.GetType().Name;
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenRegistryModel] {owner} registers {entry.ViewType.Name} without a load key. Set Screen.Load in the context.");
                return false;
            }

            (int, Type) key = (entry.Screen.ManagerId, entry.ViewType);

            if (_screens.ContainsKey(key))
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenRegistryModel] {entry.ViewType.Name} is already registered at manager {entry.Screen.ManagerId}! Updating...");

            _screens[key] = entry;
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRegistryModel] Registered screen {entry.ViewType.Name} at manager {entry.Screen.ManagerId}");
            return true;
        }

        public ScreenEntry FindEntry(Type viewType)
        {
            return _screens.Values.FirstOrDefault(entry => entry.ViewType == viewType);
        }

        public ScreenEntry GetEntry(int managerId, Type viewType)
        {
            if (viewType == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenRegistryModel] Cannot get a screen: view type is null");
                return null;
            }

            if (_screens.TryGetValue((managerId, viewType), out ScreenEntry entry))
                return entry;

            FlowLogger.LogError(SystemLogType.Screen,
                $"[ScreenRegistryModel] {viewType.Name} is not registered at manager {managerId}. Is the Root of the module that owns it in the scene?");
            return null;
        }

        public void RemoveEntry(ScreenEntry entry)
        {
            if (entry == null) return;

            if (_screens.Remove((entry.Screen.ManagerId, entry.ViewType)))
                FlowLogger.Log(SystemLogType.Screen, $"[ScreenRegistryModel] Removed screen {entry.ViewType.Name} at manager {entry.Screen.ManagerId}");
        }

        public List<ScreenEntry> GetAllEntries() => _screens.Values.ToList();

        public List<ScreenEntry> GetManagerEntries(int managerId)
        {
            return _screens.Values.Where(entry => entry.Screen.ManagerId == managerId).ToList();
        }

        public List<ScreenEntry> GetTagEntries(ScreenTag tag)
        {
            return _screens.Values.Where(entry => entry.Screen.Tag == tag).ToList();
        }

        public List<IScreenBody> GetAllLoadedScreens()
        {
            return _screens.Values.Where(entry => entry.Loaded != null).Select(entry => entry.Loaded).ToList();
        }

        public List<IScreenBody> GetAllScreensAtManager(int managerId)
        {
            return GetManagerEntries(managerId).Where(entry => entry.Loaded != null).Select(entry => entry.Loaded).ToList();
        }

        public void CopyDataFromConfig(ScreenVO screenData)
        {
            ScreenEntry entry = GetEntry(screenData.ManagerId, screenData.ScreenType);
            if (entry == null) return;

            CopyDataFromConfig(screenData, entry.Screen);
        }

        public void CopyDataFromConfig(ScreenVO screenData, ScreenCVO screen)
        {
            if (screen == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenRegistryModel] Cannot copy data: no declaration for {screenData?.ScreenType?.Name}");
                return;
            }

            screenData.LayerIndex = screen.Layer;
            screenData.Tag = screen.Tag;
            screenData.HasHideAnimation = screen.HasHideAnimation;
            screenData.HasShowAnimation = screen.HasShowAnimation;
            screenData.ForceOpenAtFullLayer = false;
            screenData.ForceOpenAtDuplication = false;
            screenData.AddToHistory = false;
            screenData.Parameters = null;
        }
    }
}
