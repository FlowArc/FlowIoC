namespace FlowIoC.BaseModule.Function.ReturnableFunctions
{
    public abstract class FunctionReturn<TReturnType> : FunctionBody, IFunctionReturn<TReturnType>
    {
        public abstract TReturnType Execute();

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = Execute();
            return true;
        }
    }

    public interface IFunctionReturn<out TReturnType> : IFunctionBody
    {
        TReturnType Execute();
    }
}
