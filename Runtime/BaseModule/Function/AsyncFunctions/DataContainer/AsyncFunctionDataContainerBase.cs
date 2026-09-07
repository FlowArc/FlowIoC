using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Provider.Coroutine;

namespace FlowIoC.BaseModule.Function.AsyncFunctions.DataContainer
{
    internal abstract class AsyncFunctionDataContainerBase : FunctionDataContainer
    {
        public ICoroutineProvider CoroutineProvider;

        /// <summary>
        /// Hands the function the callback the caller attached. Each container knows the shape of
        /// its own callback, so this is a typed assignment; the provider used to find the property
        /// by name on both objects with reflection, on every asynchronous call.
        /// </summary>
        internal abstract void ApplyCallback(AsyncFunctionBody function);
    }
}
