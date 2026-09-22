using FlowIoC.BaseModule.Contexts;

namespace Modules.BotBarModule.BotBarConnectorModule.RootsContexts
{
    /// <summary>
    /// Empty, like ConnectorContext: the wiring is in the sub-context the Root lists, and this
    /// context only gives it a Root to hang from.
    /// </summary>
    public class BotBarConnectorContext : Context
    {
    }
}