using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace Modules.CameraSystemModule.RootsContexts
{
     [CustomClassHeader("Camera", 0.1f, 0.8f, 0.9f, 0.2f, 0.2f, 0.8f, 14)]
    public class CameraRoot : Root<CameraContext>
    {
        
    }
}
