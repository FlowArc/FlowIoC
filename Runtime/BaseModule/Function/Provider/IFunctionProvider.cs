using FlowIoC.BaseModule.Function.AsyncFunctions;
using FlowIoC.BaseModule.Function.AsyncFunctions.DataContainer;

namespace FlowIoC.BaseModule.Function.Provider
{
    public interface IFunctionProvider
    {
        IFunctionDataContainer Call<TFunctionType>() where TFunctionType : IFunctionBody;
        IAsyncFunctionDataContainer CallAsync<TFunctionType>() where TFunctionType : IAsyncFunction;
        IAsyncFunctionDataContainer<TParam1> CallAsync<TFunctionType, TParam1>() where TFunctionType : IAsyncFunction<TParam1>;
        
        void ReleaseFunctionManually(IFunctionBody function);
    }
}