namespace FlowIoC.BaseModule.Function.ReturnableFunctions
{
    public abstract class FunctionReturn<TReturnType, TParam1, TParam2, TParam3> : FunctionBody, IFunctionReturn<TReturnType, TParam1, TParam2, TParam3>
    {
        public abstract TReturnType Execute(TParam1 param1, TParam2 param2, TParam3 param3);
    }
    
    public interface IFunctionReturn<out TReturnType, in TParam1, in TParam2, in TParam3> : IFunctionBody
    {
        TReturnType Execute(TParam1 param1, TParam2 param2, TParam3 param3);
    }
}