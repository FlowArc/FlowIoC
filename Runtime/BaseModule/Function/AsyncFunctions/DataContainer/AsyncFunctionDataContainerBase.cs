using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Provider.Coroutine;

namespace FlowIoC.BaseModule.Function.AsyncFunctions.DataContainer
{
    internal class AsyncFunctionDataContainerBase : FunctionDataContainer
    {
        public ICoroutineProvider CoroutineProvider;
    }
}