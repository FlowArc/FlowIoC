using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
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
            Run(LoadEntries(_registry.GetAllEntries(), completeCallback, loadingProgressCallback), "All");
        }

        public void ScreensAtManager(int managerId = 0, bool isTest = false, Action completeCallback = null,
            Action<int, int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.ScreensAtManager][manager({managerId})]");
            Run(LoadEntries(_registry.GetManagerEntries(managerId), completeCallback, loadingProgressCallback),
                "ScreensAtManager");
        }

        public void ByTag(ScreenTag tag, bool isTest = false, Action completeCallback = null,
            Action<int, int> loadingProgressCallback = null)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.ByTag][tag({tag})]");
            Run(LoadEntries(_registry.GetTagEntries(tag), completeCallback, loadingProgressCallback), "ByTag");
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
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.LoadEntries][screen({entry.ViewType.Name})][state(alreadyLoaded)]");
                    loadingProgressCallback?.Invoke(index, entries.Count);
                    continue;
                }

                IScreenBody screen = await LoadScreen(entry);
                if (screen == null) continue;

                // The load was out for frames, and an Open may have joined it - or started it, before
                // this pass reached the entry - and landed first: the instance is on stage now, and a
                // screen on stage is loaded already and not the pool's. Parking it re-parented the
                // very screen the loading bar was being drawn on. A screen a second pass parked
                // meanwhile is left where it is for the same reason.
                if (screen.Data.HasState(ScreenState.InUse) || screen.Data.HasState(ScreenState.InPool))
                {
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.LoadEntries][screen({entry.ViewType.Name})][state(loadedMeanwhile)]");
                    loadingProgressCallback?.Invoke(index, entries.Count);
                    continue;
                }

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
        /// One load per entry at a time. A caller that reaches an entry whose load is already out -
        /// the preload arriving at a screen an Open is loading, or an Open asking for a screen the
        /// preload is on - awaits that load instead of starting a second, which used to instantiate
        /// a second copy for a Resource screen and, for an addressable one, hand the second caller
        /// the first caller's instance to do the wrong thing with.
        /// </summary>
        private Task<IScreenBody> LoadScreen(ScreenEntry entry)
        {
            if (entry.Loading != null && !entry.Loading.IsCompleted)
                return entry.Loading;

            entry.Loading = LoadScreenOnce(entry);
            return entry.Loading;
        }

        /// <summary>
        /// Loads by whichever kind the declaration names, then tells the instance's ViewInjector
        /// which context owns it. The instance is parented under a ScreenRoot layer later, and
        /// bubbling up from there would find ScreenRoot's context, which knows nothing about this
        /// view; the owner is the context that bound its mediator.
        /// </summary>
        private async Task<IScreenBody> LoadScreenOnce(ScreenEntry entry)
        {
            IScreenBody screen;

            switch (entry.Screen.Load.Kind)
            {
                case ScreenLoadType.Addressable:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.LoadScreen][key({entry.Screen.Load.Key})][via(addressable)]");
                    screen = await _addressableLoadService.LoadScreen(entry);
                    break;

                case ScreenLoadType.Resource:
                    FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Load.LoadScreen][key({entry.Screen.Load.Key})][via(resource)]");
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