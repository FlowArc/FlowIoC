using FlowIoC.BaseModule.Signals;

namespace FlowIoC.Samples.CommandExecutionTestModule.Signals
{
    public class CommandTestSignals : ISignalHolder
    {
        public InComingSignals InComing = new();
        public OutGoingSignals OutGoing = new();

        public class InComingSignals
        {
            public Signal InitializeTests = new();
            public Signal StartSequenceTest = new();
            public Signal StartParallelTest = new();
            public Signal StartGroupTest = new();
            public Signal StartComplexGroupTest = new();
            public Signal StartJumpSignalTest = new();
        }

        public class OutGoingSignals
        {
            public Signal<string> TestCompleted = new();
        }
    }
}