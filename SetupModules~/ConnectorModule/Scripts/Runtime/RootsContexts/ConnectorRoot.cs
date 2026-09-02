using FlowIoC.BaseModule.Root;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The one Root that wires modules to each other. Its bar wears the Connector colour rather
    /// than a Root's, which the name alone is enough to decide - a Root takes the colour of
    /// whatever it roots.
    /// </summary>
    public class ConnectorRoot : Root<ConnectorContext>
    {
    }
}
