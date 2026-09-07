namespace FlowIoC.BaseModule.Function.VoidFunctions
{
    public abstract class FunctionVoid<TParam1, TParam2> : FunctionBody, IFunctionVoid<TParam1, TParam2>
    {
        public abstract void Execute(TParam1 param1, TParam2 param2);

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            if (!FunctionArguments.HasArity(this, parameters, 2)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 0, out TParam1 param1)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 1, out TParam2 param2)) return true;

            Execute(param1, param2);
            return true;
        }
    }

    public interface IFunctionVoid<in TParam1, in TParam2>
    {
        void Execute(TParam1 param1, TParam2 param2);
    }
}
