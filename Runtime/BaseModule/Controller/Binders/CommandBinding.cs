using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Binders
{
    public class CommandBinding : Binding, ICommandBinding
    {
        public IContext Context { get; protected set; }
        private List<CommandStepVO> _steps = new ();

        /// <summary>
        /// Where this binding was declared - the Bind line in the Context. Carried so a signal
        /// dispatched from inside the sequence can point at the sequence rather than at whatever
        /// started the chain. What the compiler wrote into the call, so it costs nothing.
        /// </summary>
        internal string DeclarationFile { get; private set; }

        internal int DeclarationLine { get; private set; }

        internal void SetDeclaration(string file, int line)
        {
            DeclarationFile = file;
            DeclarationLine = line;
        }

        internal void SetContext(IContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context), "Context cannot be null.");

            Context = context;
        }

        public ICommandBinding ToSequence<T>() where T : CommandBody, new()
        {
            _steps.Add(new CommandStepVO
            {
                CommandType = typeof(T),
                ExecutionType = CommandExecutionType.Sequence
            });
            return this;
        }

        public ICommandBinding ToSequence<T>(params object[] parameters) where T : CommandBody, new()
        {
            _steps.Add(new CommandStepVO
            {
                CommandType = typeof(T),
                ExecutionType = CommandExecutionType.Sequence,
                CommandParameters = parameters
            });
            return this;
        }

        public ICommandBinding ToParallel<T>() where T : CommandBody, new()
        {
            _steps.Add(new CommandStepVO
            {
                CommandType = typeof(T),
                ExecutionType = CommandExecutionType.Parallel
            });
            return this;
        }

        public ICommandBinding ToParallel<T>(params object[] parameters) where T : CommandBody, new()
        {
            _steps.Add(new CommandStepVO
            {
                CommandType = typeof(T),
                ExecutionType = CommandExecutionType.Parallel,
                CommandParameters = parameters
            });
            return this;
        }

        public ICommandBinding ToGroupAsSequence(ISignalBody key, params object[] signalParameters)
        {
            var step = new CommandStepVO
            {
                CommandType = null,
                ExecutionType = CommandExecutionType.Sequence,
                GroupKey = key,
                SignalParameters = signalParameters
            };
            _steps.Add(step);
            return this;
        }
        
        public ICommandBinding ToGroupAsParallel(ISignalBody key, params object[] signalParameters)
        {
            var step = new CommandStepVO
            {
                CommandType = null,
                ExecutionType = CommandExecutionType.Parallel,
                GroupKey = key,
                SignalParameters = signalParameters
            };
            _steps.Add(step);
            return this;
        }
        public List<CommandStepVO> GetCommandSteps()
        {
            return _steps;
        }
        public override void Clear()
        {
            Context = null;
            _steps.Clear();
            base.Clear();
        }
    }
}