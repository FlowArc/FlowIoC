#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;

namespace Modules.BotBarModule.BotBarTestModule.RootsContexts
{
    /// <summary>Order 50: after BotBarSystemRoot, whose holder this context asks for in Setup.</summary>
    public class BotBarTestRoot : Root<BotBarTestContext>
    {
    }
}
#endif
