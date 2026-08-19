using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Controller.Commands
{
    public class DispatchSignalCommand : Command<Signal>
    {
        public override void Execute(Signal signal)
        {
            Retain();
            signal.Dispatch();
            Release();
        }
    }
    public class DispatchSignalCommand<T1> : Command<Signal<T1>, T1>
    {
        public override void Execute(Signal<T1> signal, T1 param)
        {
            Retain();
            signal.Dispatch(param);
            Release();
        }
    }
    public class DispatchSignalCommand<T1, T2> : Command<Signal<T1, T2>, T1, T2>
    {
        public override void Execute(Signal<T1,T2> signal, T1 param1, T2 param2)
        {
            Retain();
            signal.Dispatch(param1, param2);
            Release();
        }
    }
    public class DispatchSignalCommand<T1, T2, T3> : Command<Signal<T1, T2, T3>, T1, T2, T3>
    {
        public override void Execute(Signal<T1,T2, T3> signal, T1 param1, T2 param2, T3 param3)
        {
            Retain();
            signal.Dispatch(param1, param2, param3);
            Release();
        }
    }
}