using FlowIoC.BaseModule.Root;

namespace FlowIoC.ScreenModule.RootsContexts
{
    public class BaseScreenRoot<TContext> : Root<TContext>
        where TContext : BaseScreenContext, new()
    {
    }
}