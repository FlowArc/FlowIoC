using FlowIoC.BaseModule.Function.AsyncFunctions;
using FlowIoC.BaseModule.Function.AsyncFunctions.DataContainer;

namespace FlowIoC.BaseModule.Function.Provider
{
    public interface IFunctionProvider
    {
        IFunctionDataContainer Execute<TFunctionType>() where TFunctionType : IFunctionBody;
        IAsyncFunctionDataContainer ExecuteAsync<TFunctionType>() where TFunctionType : IAsyncFunction;
        IAsyncFunctionDataContainer<TParam1> ExecuteAsync<TFunctionType, TParam1>() where TFunctionType : IAsyncFunction<TParam1>;
        
        void ReleaseFunctionManually(IFunctionBody function);
    }
}