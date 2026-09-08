using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Bind.Binders;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Binders
{
    public interface ICommandBinder : IBinder<CommandBinding>
    {
        /// <summary>
        /// The file and line are the ones the compiler writes into the call - where this binding is
        /// declared. A signal dispatched from inside the sequence points there rather than at
        /// whatever started the chain, and it costs nothing to know.
        /// </summary>
        public ICommandBinding Bind<TSignal>(TSignal key,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
            where TSignal : ISignalBody;
    }
}