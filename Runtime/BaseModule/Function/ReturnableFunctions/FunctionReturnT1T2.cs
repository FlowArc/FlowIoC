namespace FlowIoC.BaseModule.Function.ReturnableFunctions
{
    public abstract class FunctionReturn<TReturnType, TParam1, TParam2> : FunctionBody, IFunctionReturn<TReturnType, TParam1, TParam2>
    {
        public abstract TReturnType Execute(TParam1 param1, TParam2 param2);

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            if (!FunctionArguments.HasArity(this, parameters, 2)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 0, out TParam1 param1)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 1, out TParam2 param2)) return true;

            result = Execute(param1, param2);
            return true;
        }
    }

    public interface IFunctionReturn<out TReturnType, in TParam1, in TParam2> : IFunctionBody
    {
        TReturnType Execute(TParam1 param1, TParam2 param2);
    }
}
