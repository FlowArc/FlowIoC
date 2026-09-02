using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    public class UnloadSubService
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private IScreenRuntimeModel _runtimeModel { get; set; }
        [Inject] private DisposeSubService _dispose { get; set; }
        [Inject] private HideSubService _hide { get; set; }

        public void AllScreens(bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Close.AllScreens][isForce({isForce})]");

            foreach (var screenBody in _registry.GetAllLoadedScreens())
            {
                Screen(screenBody, isForce);
            }
        }

        public void ScreensByManager(int managerId, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Close.AllScreensAtManager][isForce({isForce}) manager({managerId})]");

            foreach (var screenBody in _registry.GetAllScreensAtManager(managerId))
            {
                Screen(screenBody, isForce);
            }
        }

        public void ScreensByTag(ScreenTag tag, bool isForce = false)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Unload.ScreensByTag][isForce({isForce}) tag:({tag})]");

            foreach (ScreenEntry entry in _registry.GetTagEntries(tag))
            {
                if (entry.Loaded != null)
                    Screen(entry.Loaded, isForce);
            }
        }

        public void Screen<T>(int managerId = 0, bool isForce = false) where T : IScreenBody
        {
            ScreenEntry entry = _registry.GetEntry(managerId, typeof(T));

            if (entry?.Loaded != null)
                Screen(entry.Loaded, isForce);
            else
                FlowLogger.LogWarning(SystemLogType.Screen, $"[ScreenService.Unload] {typeof(T).Name} is not loaded at manager({managerId})!");
        }

        public void Screen(IScreenBody screenBody, bool isForce = false)
        {
            screenBody.Data.AddState(ScreenState.Unloading);
            _hide.Screen(screenBody, isForce);
        }

        /// <summary>
        /// The screen's context is going away. An active screen goes through the hide path with
        /// the animation skipped; a pooled one is disposed directly, because Hide refuses a screen
        /// that is not in use. Both end in the loader releasing the instance.
        /// </summary>
        internal void Unregistered(IScreenBody screenBody)
        {
            if (screenBody.Data.HasState(ScreenState.InUse))
            {
                Screen(screenBody, isForce: true);
                return;
            }

            RemoveFromPools(screenBody);
            _dispose.Screen(screenBody);
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