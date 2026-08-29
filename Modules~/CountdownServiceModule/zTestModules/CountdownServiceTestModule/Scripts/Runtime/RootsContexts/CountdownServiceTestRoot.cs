#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.RootsContexts
{
     [CustomClassHeader("ROOTs", 0.8f, 0.2f, 0.2f, 0.2f, 0.2f, 0.8f, 14)]
    public class CountdownServiceTestRoot : Root<CountdownServiceTestContext>
    {
        
    }
}
#endif
