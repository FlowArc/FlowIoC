#if UNITY_EDITOR
using FlowIoC.ScreenModule.RootsContexts;
using Modules.WorldPointerModule.WorldPointerTestModule.Controllers;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;
using Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerTestModule.RootsContexts
{
    /// <summary>
    /// The world side of the sample: cubes, buttons, and the commands that register the cubes
    /// and send them requests. The display is the sample screen, listed on this Root beside it
    /// and opened in Launch through the screen service like any screen. The ScreenManager hangs
    /// under this Root, so the context is a BaseScreenContext, which mediates it.
    /// </summary>
    public class WorldPointerTestContext : BaseScreenContext
    {
        private WorldPointerTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<WorldPointerTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<WorldPointerTestView>().To<WorldPointerTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.OpenSampleScreen).ToSequence<ToggleSampleScreenCommand>();
            CommandBinder.Bind(_signals.ToggleSampleScreen).ToSequence<ToggleSampleScreenCommand>();
            CommandBinder.Bind(_signals.RegisterPointers).ToSequence<RegisterTestPointersCommand>();
            CommandBinder.Bind(_signals.ChangeContent).ToSequence<ChangeTestContentCommand>();
            CommandBinder.Bind(_signals.SetHidden).ToSequence<SetTestPointersHiddenCommand>();
            CommandBinder.Bind(_signals.UnregisterPointers).ToSequence<UnregisterTestPointersCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.OpenSampleScreen.Dispatch();
        }
    }
}
#endif