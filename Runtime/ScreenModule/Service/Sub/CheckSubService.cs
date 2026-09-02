using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    public class CheckSubService
    {
        [Inject] private IScreenRuntimeModel _runtime { get; set; }

        public bool IsScreenActive(IScreenBody screenBody, int managerId = 0)
        {
            return _runtime.IsScreenActive(screenBody.Data.ScreenType, managerId, out _);
        }

        public bool IsScreenActive<T>(int managerId = 0) where T : IScreenBody
        {
            return _runtime.IsScreenActive(typeof(T), managerId, out _);
        }

        public bool IsLayerFull(int layerIndex, int managerId = 0)
        {
            return _runtime.IsLayerFull(layerIndex, managerId, out _);
        }
    }
}