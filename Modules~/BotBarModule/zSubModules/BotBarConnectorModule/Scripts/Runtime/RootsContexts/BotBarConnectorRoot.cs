using FlowIoC.BaseModule.Root;

namespace Modules.BotBarModule.BotBarConnectorModule.RootsContexts
{
    /// <summary>
    /// The bar's own Connector seat, a child of BotBarSystemRoot so a game drops one prefab. Its
    /// name alone gives it the Connector colour.
    /// </summary>
    public class BotBarConnectorRoot : Root<BotBarConnectorContext>
    {
    }
}