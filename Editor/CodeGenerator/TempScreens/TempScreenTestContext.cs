using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace FlowIoC.Editor.CodeGenerator.TempScreens
{
    [ExcludeFromContextWindow]
    internal class TempScreenTestContext : BaseScreenContext
    {

        public override void SignalBindings()
        {
            base.SignalBindings();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}