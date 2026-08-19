using System.Collections;

namespace FlowIoC.BaseModule.Function.AsyncFunctions
{
    public abstract class AsyncFunctionBody : FunctionBody
    {
        public abstract IEnumerator Execute();
    }
}