namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command : CommandBody, ICommand
    {
        public abstract void Execute();
    }

    public interface ICommand : ICommandBody
    {
        void Execute();
    }
}