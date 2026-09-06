namespace FlowIoC.BaseModule.Controller
{
    public abstract class Command<T1, T2, T3, T4> : CommandBody, ICommand<T1, T2, T3, T4>
    {
        public abstract void Execute(T1 param1, T2 param2, T3 param3, T4 param4);

        internal override void InvokeExecute(object[] parameters)
        {
            if (!HasArity(parameters, 4)) return;
            if (!TryFill(parameters, 0, out T1 param1)) return;
            if (!TryFill(parameters, 1, out T2 param2)) return;
            if (!TryFill(parameters, 2, out T3 param3)) return;
            if (!TryFill(parameters, 3, out T4 param4)) return;

            Execute(param1, param2, param3, param4);
        }
    }

    public interface ICommand<in T1, in T2, in T3, in T4> : ICommandBody
    {
        void Execute(T1 param1, T2 param2, T3 param3, T4 param4);
    }
}
