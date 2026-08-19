using System;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Binders
{
    public class CommandStepVO
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Type CommandType;
        public CommandExecutionType ExecutionType;
        public ISignalBody GroupKey; // To trigger another binding group
        public object[] SignalParameters; // Parameters for ToGroup signal
        public object[] CommandParameters; // Parameters to pass to the command
    }
}