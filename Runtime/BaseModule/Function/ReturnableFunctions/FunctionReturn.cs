namespace FlowIoC.BaseModule.Function.ReturnableFunctions
{
    public abstract class FunctionReturn<TReturnType> : FunctionBody, IFunctionReturn<TReturnType>
    {
        public abstract TReturnType Execute();
    }

    public interface IFunctionReturn<out TReturnType> : IFunctionBody
    {
        TReturnType Execute();
    }
}