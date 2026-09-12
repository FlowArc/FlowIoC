#if UNITY_EDITOR
using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.LoadingModule.Controllers;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.LoadingTestModule.Controllers;
using Modules.LoadingModule.LoadingTestModule.Signals;

namespace Modules.LoadingModule.LoadingTestModule.RootsContexts
{
    /// <summary>
    /// Every state of the two screens, once: a fullscreen set with parallel steps and a child set
    /// on the second bar, a step that fails and a retry that succeeds, and a silent set running
    /// beside it all. The config is CD_LoadingSets_Test on this scene's LoadingServiceRoot.
    /// </summary>
    public class LoadingTestContext : Context
    {
        private LoadingTestInternalSignals _internalSignals;
        private LoadingScreenSignals _loadingScreenSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<LoadingTestInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.Launch)
                .ToSequence<BeginLoadingCommand>("TestBoot")
                .ToSequence<DispatchSignalCommand>(_internalSignals.RunBackground)
                .ToParallel<FakeLoadingStepCommand>("TestScreens", 3f, false)
                .ToParallel<FakeLoadingStepCommand>("TestPools", 5f, false)
                .ToGroupAsSequence(_internalSignals.RunDownload)
                .ToSequence<FakeLoadingStepCommand>("TestProfile", 2f, true) // fails the first time; the retry re-runs it alone
                .ToSequence<AwaitLoadingCommand>("TestBoot");

            // The child set: two steps on the second bar.
            CommandBinder.Bind(_internalSignals.RunDownload)
                .ToSequence<FakeLoadingStepCommand>("TestCatalog", 1f, false)
                .ToSequence<FakeLoadingStepCommand>("TestBundles", 4f, false);

            // Silent, beside the boot; nothing on screen, everything in the console.
            CommandBinder.Bind(_internalSignals.RunBackground)
                .ToParallel<FakeLoadingStepCommand>("TestAds", 2f, false)
                .ToSequence<FakeLoadingStepCommand>("TestClan", 6f, false);

            // The retry: the failed step alone; the set reopens and completes on its own.
            CommandBinder.Bind(_internalSignals.Retry)
                .ToSequence<FakeLoadingStepCommand>("TestProfile", 2f, false);
        }

        public override void Setup()
        {
            base.Setup();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();
            _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _internalSignals.Retry.Dispatch());
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Launch.Dispatch();
        }

        public override void DestroyContext()
        {
            _loadingScreenSignals?.Outgoing.RetryClicked.Disconnect();
            base.DestroyContext();
        }
    }
}
#endif