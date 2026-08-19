namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command<T1> : CommandBody, ICommand<T1>
    {
        public abstract void Execute(T1 param1);
    }
    
    public interface ICommand<in T1> : ICommandBody
    {
        void Execute(T1 param1);
    }
}