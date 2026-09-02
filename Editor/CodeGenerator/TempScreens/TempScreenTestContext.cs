using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace FlowIoC.Editor.CodeGenerator.TempScreens
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    internal class TempScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
        }
    }
}
