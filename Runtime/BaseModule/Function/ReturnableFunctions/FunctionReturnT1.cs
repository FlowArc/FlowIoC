namespace FlowIoC.BaseModule.Function.ReturnableFunctions
{
    public abstract class FunctionReturn<TReturnType, TParam1> : FunctionBody, IFunctionReturn<TReturnType, TParam1>
    {
        public abstract TReturnType Execute(TParam1 param1);

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            if (!FunctionArguments.HasArity(this, parameters, 1)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 0, out TParam1 param1)) return true;

            result = Execute(param1);
            return true;
        }
    }

    public interface IFunctionReturn<out TReturnType, in TParam1> : IFunctionBody
    {
        TReturnType Execute(TParam1 param1);
    }
}
