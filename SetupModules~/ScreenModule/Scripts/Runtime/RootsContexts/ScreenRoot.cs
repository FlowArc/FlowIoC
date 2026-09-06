using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.ScreenModule.RootsContexts
{
    /// <summary>
    /// The Root the whole screen mechanism turns on, and a project needs at least one
    /// <c>ScreenManager</c> under it before any UI can be shown. It is the frame rather than a
    /// feature, so its bar wears the Core colour - which only the attribute can give it, because a
    /// Core module carries no suffix for the name to be read from.
    /// </summary>
    [FlowHeader(FlowRole.Core)]
    public class ScreenRoot : BaseScreenRoot<ScreenContext>
    {
    }
}