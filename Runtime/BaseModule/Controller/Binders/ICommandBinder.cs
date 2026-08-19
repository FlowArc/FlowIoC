using FlowIoC.BaseModule.Bind.Binders;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Binders
{
    public interface ICommandBinder : IBinder<CommandBinding>
    {
        public ICommandBinding Bind<TSignal>(TSignal key)
            where TSignal : ISignalBody;
    }
}