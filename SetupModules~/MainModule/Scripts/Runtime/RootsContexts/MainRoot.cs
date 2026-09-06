using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace Modules.MainModule.RootsContexts
{
    /// <summary>
    /// The Root the project starts from, and the last <c>Launch</c> to run. It is the frame rather
    /// than a feature, so its bar wears the Core colour - which only the attribute can give it,
    /// because a Core module carries no suffix for the name to be read from.
    /// </summary>
    [FlowHeader(FlowRole.Core)]
    public class MainRoot : Root<MainContext>
    {
    }
}