using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Binders
{
    public interface ICommandBinding
    {
        public object Key { get; }
        public object Value { get; }

        IContext Context { get; }

        ICommandBinding ToSequence<T>() where T : ICommandBody;
        ICommandBinding ToSequence<T>(params object[] parameters) where T : ICommandBody;
        ICommandBinding ToParallel<T>() where T : ICommandBody;
        ICommandBinding ToParallel<T>(params object[] parameters) where T : ICommandBody;
        ICommandBinding ToGroupAsSequence(ISignalBody key, params object[] signalParameters);
        ICommandBinding ToGroupAsParallel(ISignalBody key, params object[] signalParameters);

        List<CommandStepVO> GetCommandSteps();
    }
}