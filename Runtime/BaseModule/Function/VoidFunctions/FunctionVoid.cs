namespace FlowIoC.BaseModule.Function.VoidFunctions
{
    public abstract class FunctionVoid : FunctionBody, IFunctionVoid
    {
        public abstract void Execute();

        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            Execute();
            return true;
        }
    }

    public interface IFunctionVoid
    {
        void Execute();
    }
}
