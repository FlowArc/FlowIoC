using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace Modules.CountdownServiceModule.RootsContexts
{
     [CustomClassHeader("ROOTs", 0.8f, 0.2f, 0.2f, 0.2f, 0.2f, 0.8f, 14)]
    public class CountdownServiceRoot : Root<CountdownServiceContext>
    {
        
    }
}
