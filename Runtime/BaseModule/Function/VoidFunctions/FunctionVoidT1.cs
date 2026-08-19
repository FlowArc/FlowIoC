namespace FlowIoC.BaseModule.Function.VoidFunctions
{
    public abstract class FunctionVoid<TParam1> : FunctionBody, IFunctionVoid<TParam1>
    {
        public abstract void Execute(TParam1 param1);
    }

    public interface IFunctionVoid<in TParam1>
    {
        void Execute(TParam1 param1);
    }
}