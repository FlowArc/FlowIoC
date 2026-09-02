using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service
{

    internal class ScreenService : IScreenService
    {
        [Inject] private IScreenBuilderSubService _builder { get; set; }
        [Inject] public LoadSubService Load { get; set; }
        [Inject] public CheckSubService Check { get; set; }
        [Inject] public TryGetSubService TryGet { get; set; }
        [Inject] public HideSubService Hide { get; set; }
        [Inject] public UnloadSubService Unload { get; set; }

        public IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody
        {
            return _builder.Open<T>(managerId);
        }

    }
}