using FlowIoC.BaseModule.Signals;

namespace FlowIoC.Samples.CommandExecutionTestModule.Signals
{
    public class CommandTestSignalsInternal : ISignalHolder
    {
        
        public Signal TriggerGroupA = new();
        public Signal TriggerGroupB = new();
        public Signal TriggerGroupC = new();
    }
}