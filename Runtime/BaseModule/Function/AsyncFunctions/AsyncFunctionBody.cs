using System.Collections;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Function.AsyncFunctions
{
    public abstract class AsyncFunctionBody : FunctionBody
    {
        public abstract IEnumerator Execute();

        /// <summary>
        /// An async function is not run this way. It is driven as a coroutine by
        /// ExecuteAsyncFunction, which yields on its own typed Execute, so reaching here means the
        /// caller used Call where CallAsync was meant - and the function would otherwise do
        /// nothing at all with no report.
        /// </summary>
        internal override bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;

            FlowLogger.LogError(SystemLogType.Function,
                GetType().Name + " is an async function, so it is called with CallAsync rather than Call.", GetType());

            return true;
        }
    }
}