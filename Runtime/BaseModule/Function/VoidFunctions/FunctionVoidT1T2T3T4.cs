namespace FlowIoC.BaseModule.Function.VoidFunctions
{
    public abstract class FunctionVoid<TParam1, TParam2, TParam3, TParam4> : FunctionBody, IFunctionVoid<TParam1, TParam2, TParam3, TParam4>
    {
        public abstract void Execute(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            if (!FunctionArguments.HasArity(this, parameters, 4)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 0, out TParam1 param1)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 1, out TParam2 param2)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 2, out TParam3 param3)) return true;
            if (!FunctionArguments.TryFill(this, parameters, 3, out TParam4 param4)) return true;

            Execute(param1, param2, param3, param4);
            return true;
        }
    }

    public interface IFunctionVoid<in TParam1, in TParam2, in TParam3, in TParam4>
    {
        void Execute(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);
    }
}
