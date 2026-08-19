using FlowIoC.BaseModule.Contexts;
using FlowIoC.Samples.CommandExecutionTestModule.Controllers;
using FlowIoC.Samples.CommandExecutionTestModule.Signals;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.RootsContexts
{
    public class CommandTestContext : Context
    {
        private CommandTestSignals _testSignals;
        private CommandTestSignalsInternal _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _testSignals = InjectionBinder.Bind<CommandTestSignals>();
            _internalSignals = InjectionBinder.Bind<CommandTestSignalsInternal>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            
            CommandBinder.Bind(_testSignals.InComing.StartSequenceTest)
                .ToSequence<ACommand3>()
                .ToSequence<BCommand7>()
                .ToSequence<CCommand0>();
            
            CommandBinder.Bind(_testSignals.InComing.StartParallelTest)
                .ToParallel<DCommand12>()
                .ToParallel<ECommand6>()
                .ToParallel<FCommand2>();
            
            CommandBinder.Bind(_testSignals.InComing.StartGroupTest)
                .ToSequence<ACommand3>()
                .ToGroupAsSequence(_internalSignals.TriggerGroupA)
                .ToSequence<ACommand3>()
                .ToGroupAsSequence(_internalSignals.TriggerGroupB);

            CommandBinder.Bind(_internalSignals.TriggerGroupA).ToSequence<GroupCommandA>();
            
            CommandBinder.Bind(_internalSignals.TriggerGroupB).ToSequence<GroupCommandB>();

            CommandBinder.Bind(_testSignals.InComing.StartComplexGroupTest)
                .ToSequence<ACommand3>()
                .ToParallel<ECommand6>()
                .ToSequence<BCommand7>()
                .ToGroupAsSequence(_internalSignals.TriggerGroupC)
                .ToSequence<ACommand3>();
            
            CommandBinder.Bind(_internalSignals.TriggerGroupC)
                .ToSequence<FCommand2>()
                .ToParallel<CCommand0>()
                .ToGroupAsSequence(_internalSignals.TriggerGroupB)
                .ToParallel<ECommand6>()
                .ToSequence<BCommand7>();

            CommandBinder.Bind(_testSignals.InComing.StartJumpSignalTest)
                .ToSequence<GCommand1>(true,
                    _internalSignals.TriggerGroupA,
                    _internalSignals.TriggerGroupB);
            
            // CommandBinder.Bind(_internalSignals.TriggerGroupA)
            //     .ToGroupAsSequence(_internalSignals.TriggerGroupB)
            //     .ToGroupAsSequence(_internalSignals.TriggerGroupC);
            
            CommandBinder.Bind(_testSignals.InComing.InitializeTests)
                .ToSequence<InitializeTestCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            Debug.Log("=== CommandTestContext Launched ===");
            Debug.Log("Starting automatic test sequence...");
            
            _testSignals.InComing.InitializeTests.Dispatch();
        }
    }
}