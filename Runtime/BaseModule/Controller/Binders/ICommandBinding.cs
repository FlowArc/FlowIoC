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

        ICommandBinding ToSequence<T>() where T : CommandBody, new();
        ICommandBinding ToSequence<T>(params object[] parameters) where T : CommandBody, new();

        /// <summary>
        /// The step by its <see cref="System.Type"/>, for a step found at runtime - the on-device
        /// debug panel binding a Command it discovered by reflection. A Context writes the generic
        /// form; this one refuses a type that is not a Command.
        /// </summary>
        ICommandBinding ToSequence(System.Type commandType, params object[] parameters);
        ICommandBinding ToParallel<T>() where T : CommandBody, new();
        ICommandBinding ToParallel<T>(params object[] parameters) where T : CommandBody, new();
        ICommandBinding ToGroupAsSequence(ISignalBody key, params object[] signalParameters);
        ICommandBinding ToGroupAsParallel(ISignalBody key, params object[] signalParameters);

        List<CommandStepVO> GetCommandSteps();
    }
}