namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command : CommandBody, ICommand
    {
        public abstract void Execute();

        // The payload is ignored rather than checked: a parameterless Execute is the ordinary
        // shape for a command that reads the signal through [SignalParam] instead.
        internal override void InvokeExecute(object[] parameters) => Execute();
    }

    public interface ICommand : ICommandBody
    {
        void Execute();
    }
}
