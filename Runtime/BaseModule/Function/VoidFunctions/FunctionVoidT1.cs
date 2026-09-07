namespace FlowIoC.BaseModule.Function.VoidFunctions
{
    public abstract class FunctionVoid<TParam1> : FunctionBody, IFunctionVoid<TParam1>
    {
        public abstract void Execute(TParam1 param1);

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            if (!FunctionArguments.HasArity(this, parameters, 1)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 0, out TParam1 param1)) return true;

            Execute(param1);
            return true;
        }
    }

    public interface IFunctionVoid<in TParam1>
    {
        void Execute(TParam1 param1);
    }
}
