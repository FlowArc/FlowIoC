namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command<T1> : CommandBody, ICommand<T1>
    {
        public abstract void Execute(T1 param1);

        internal override void InvokeExecute(object[] parameters)
        {
            if (!HasArity(parameters, 1)) return;
            if (!TryFill(parameters, 0, out T1 param1)) return;

            Execute(param1);
        }
    }

    public interface ICommand<in T1> : ICommandBody
    {
        void Execute(T1 param1);
    }
}
