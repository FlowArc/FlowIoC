namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command<T1, T2, T3> : CommandBody, ICommand<T1, T2, T3>
    {
        public abstract void Execute(T1 param1, T2 param2, T3 param3);

        internal override void InvokeExecute(object[] parameters)
        {
            if (!HasArity(parameters, 3)) return;
            if (!TryFill(parameters, 0, out T1 param1)) return;
            if (!TryFill(parameters, 1, out T2 param2)) return;
            if (!TryFill(parameters, 2, out T3 param3)) return;

            Execute(param1, param2, param3);
        }
    }

    public interface ICommand<in T1, in T2, in T3> : ICommandBody
    {
        void Execute(T1 param1, T2 param2, T3 param3);
    }
}
