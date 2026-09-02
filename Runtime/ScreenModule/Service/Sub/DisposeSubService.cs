using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    internal class DisposeSubService
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private AddressableLoadSubService _addressableLoadService { get; set; }
        [Inject] private ResourceLoadSubService _resourceLoadService { get; set; }

        public void Screen(IScreenBody screenBody)
        {
            ScreenEntry entry = _registry.GetEntry(screenBody.Data.ManagerId, screenBody.Data.ScreenType);
            if (entry == null) return;

            switch (entry.Screen.Load.Kind)
            {
                case ScreenLoadType.Addressable:
                    _addressableLoadService.UnloadScreen(entry, screenBody);
                    break;

                case ScreenLoadType.Resource:
                    _resourceLoadService.UnloadScreen(entry, screenBody);
                    break;

                default:
                    FlowLogger.LogError(SystemLogType.Screen,
                        $"[ScreenService] Unknown load kind: {entry.Screen.Load.Kind} for screen {screenBody.GetType().Name}");
                    break;
            }
        }
    }
}