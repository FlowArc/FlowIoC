using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    public class LoadSubService
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService { get; set; }
        [Inject] private ResourceLoadSubService _resourceLoadSubService { get; set; }

        public void All(bool isTest = false, Action completeCallback = null, Action<int, int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenService.Load.All]");
            Run(LoadEntries(_registry.GetAllEntries(), completeCallback, loadingProgressCallback), nameof(All));
        }

        public void ScreensAtManager(int managerId = 0, bool isTest = false, Action completeCallback = null,
            Action<int, int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenService.Load.ScreensAtManager]");
            Run(LoadEntries(_registry.GetManagerEntries(managerId), completeCallback, loadingProgressCallback),
                nameof(ScreensAtManager));
        }

        public void ByTag(ScreenTag tag, bool isTest = false, Action completeCallback = null,
            Action<int, int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.ByTag] {tag}");
            Run(LoadEntries(_registry.GetTagEntries(tag), completeCallback, loadingProgressCallback), nameof(ByTag));
        }

        /// <summary>
        /// Starts a load nobody awaits. These three return void because a Command calls them and
        /// hears back through the callback - but an unguarded async void swallows whatever went
        /// wrong, so the load stopped, the callback never came, and nothing was written down.
        /// </summary>
        private async void Run(Task load, string caller)
        {
            try
            {
                await load;
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Load.{caller}] stopped: {exception.Message}\n{exception}");
            }
        }

        private async Task LoadEntries(List<ScreenEntry> entries, Action completeCallback, Action<int, int> loadingProgressCallback)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                ScreenEntry entry = entries[index];

                if (entry.Loaded != null)
                {
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Screen is already loaded: {entry.ViewType.Name}");
                    loadingProgressCallback?.Invoke(index, entries.Count);
                    continue;
                }

                IScreenBody screen = await LoadScreen(entry);
                if (screen == null) continue;

                screen.Data.ScreenType = screen.GetType();
                screen.Data.ManagerId = entry.Screen.ManagerId;
                _registry.CopyDataFromConfig(screen.Data, entry.Screen);
                _runtimeModel.AddToPassivePool(screen);
                loadingProgressCallback?.Invoke(index, entries.Count);
            }

            completeCallback?.Invoke();
        }

        internal async Task<IScreenBody> Screen(ScreenVO screenData)
        {
            ScreenEntry entry = _registry.GetEntry(screenData.ManagerId, screenData.ScreenType);
            return entry == null ? null : await LoadScreen(entry);
        }

        /// <summary>
        /// Loads by whichever kind the declaration names, then tells the instance's ViewInjector
        /// which context owns it. The instance is parented under a ScreenRoot layer later, and
        /// bubbling up from there would find ScreenRoot's context, which knows nothing about this
        /// view; the owner is the context that bound its mediator.
        /// </summary>
        private async Task<IScreenBody> LoadScreen(ScreenEntry entry)
        {
            IScreenBody screen;

            switch (entry.Screen.Load.Kind)
            {
                case ScreenLoadType.Addressable:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Addressable load for {entry.Screen.Load.Key}");
                    screen = await _addressableLoadService.LoadScreen(entry);
                    break;

                case ScreenLoadType.Resource:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load] Resource load for {entry.Screen.Load.Key}");
                    screen = await _resourceLoadSubService.LoadScreen(entry);
                    break;

                default:
                    FlowLogger.LogError(SystemLogType.Screen,
                        $"[ScreenService.Load] Unknown load kind {entry.Screen.Load.Kind} for screen {entry.ViewType.Name}");
                    return null;
            }

            if (screen == null)
                return null;

            ViewInjector injector = screen.transform.GetComponent<ViewInjector>();
            if (injector == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Load] {entry.ViewType.Name}'s prefab has no ViewInjector, so its mediator cannot be registered.");
                return screen;
            }

            injector.AssignContext(entry.Owner);
            return screen;
        }
    }
}