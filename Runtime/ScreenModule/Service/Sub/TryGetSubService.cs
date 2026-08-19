using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub
{
    public class TryGetSubService
    {
        [Inject] private IScreenRuntimeModel _runtime { get; set; }
        
        public bool Screen<T>(out T screenBody, int managerId = 0) where T : IScreenBody
        {
            var isActive = _runtime.IsScreenActive(typeof(T), managerId, out var screen);
            screenBody = (T) screen;
            return isActive;
        }
        public bool ScreenInLayer(int layerIndex, out IScreenBody screenBody, int managerId = 0)
        {
            var isActive = _runtime.IsLayerFull(layerIndex, managerId, out var screen);
            screenBody = screen;
            return isActive;
        }
    }
}