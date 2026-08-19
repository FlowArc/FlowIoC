using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Function.AsyncFunctions;
using FlowIoC.BaseModule.Function.AsyncFunctions.DataContainer;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Function.Provider
{
    [HideInModelViewer]
    public class FunctionProvider : IFunctionProvider
    {
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }

        private readonly Dictionary<Type, Stack<FunctionDataContainer>> _functionDataContainerPool;
        private readonly Dictionary<Type, Stack<IFunctionBody>> _functionPool;

        internal IContext Context;

        public FunctionProvider()
        {
            _functionDataContainerPool = new Dictionary<Type, Stack<FunctionDataContainer>>();
            _functionPool = new Dictionary<Type, Stack<IFunctionBody>>();
        }

        private FunctionDataContainer GetFunctionDataContainer<TDataContainerType>() where TDataContainerType : FunctionDataContainer, new()
        {
            Type dataContainerType = typeof(TDataContainerType);
            if (!_functionDataContainerPool.TryGetValue(dataContainerType, out Stack<FunctionDataContainer> pool))
            {
                pool = new Stack<FunctionDataContainer>();
                _functionDataContainerPool[dataContainerType] = pool;
            }

            FunctionDataContainer availableFunctionDataContainer = pool.Count > 0 ? pool.Pop() : new TDataContainerType();

            availableFunctionDataContainer.FunctionProvider = this;
            if (availableFunctionDataContainer is AsyncFunctionDataContainerBase asyncDataContainer)
            {
                asyncDataContainer.CoroutineProvider = _coroutineProvider;
            }

            return availableFunctionDataContainer;
        }

        public IFunctionDataContainer Execute<TFunctionType>() where TFunctionType : IFunctionBody
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<FunctionDataContainer>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer;
        }

        public IAsyncFunctionDataContainer ExecuteAsync<TFunctionType>() where TFunctionType : IAsyncFunction
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<AsyncFunctionDataContainer>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer as IAsyncFunctionDataContainer;
        }

        public IAsyncFunctionDataContainer<TParam1> ExecuteAsync<TFunctionType, TParam1>() where TFunctionType : IAsyncFunction<TParam1>
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<AsyncFunctionDataContainer<TParam1>>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer as IAsyncFunctionDataContainer<TParam1>;
        }

        internal void ExecuteFunction(FunctionDataContainer functionDataContainer)
        {
            IFunctionBody function = GetFunction(functionDataContainer);
            MethodInfo executeMethodInfo = functionDataContainer.FunctionType.GetMethod("Execute");

            Context.TryToInjectFunction(function);
            executeMethodInfo?.Invoke(function, functionDataContainer.ExecuteParameters);
            FlowLogger.LogWarning(SystemLogType.Function, "Function Executed! " + function.GetType().Name);
            
            if (!function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }
            
            functionDataContainer.Dispose();
            _functionDataContainerPool[functionDataContainer.GetType()].Push(functionDataContainer);
        }

        internal TReturnType ExecuteFunction<TReturnType>(FunctionDataContainer functionDataContainer)
        {
            IFunctionBody function = GetFunction(functionDataContainer);
            MethodInfo executeMethodInfo = functionDataContainer.FunctionType.GetMethod("Execute");

            Context.TryToInjectFunction(function);
            object result = executeMethodInfo?.Invoke(function, functionDataContainer.ExecuteParameters);
            FlowLogger.LogWarning(SystemLogType.Function, "Function Executed! " + function.GetType().Name);

            functionDataContainer.Dispose();
            _functionDataContainerPool[functionDataContainer.GetType()].Push(functionDataContainer);
            
            if (!function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }
            
            return (TReturnType)result;
        }

        internal IEnumerator ExecuteAsyncFunction(FunctionDataContainer functionDataContainer)
        {
            AsyncFunctionBody function = GetFunction(functionDataContainer) as AsyncFunctionBody;
            object functionCompletedCallback = functionDataContainer.GetType().GetProperty("FunctionCompletedCallback")?.GetValue(functionDataContainer);

            function?.GetType().GetProperty("FunctionCompletedCallback")?.SetValue(function, functionCompletedCallback);
            Context.TryToInjectFunction(function);
            yield return function?.Execute();
            FlowLogger.LogWarning(SystemLogType.Function, "Function Executed! " + function?.GetType().Name);
            
            if (function != null && !function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }
            
            functionDataContainer.Dispose();
            _functionDataContainerPool[functionDataContainer.GetType()].Push(functionDataContainer);
        }

        public void ReleaseFunctionManually(IFunctionBody function)
        {
            if (!function.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.Function, $"Function must be retained to call manual RELEASE! Function: {function.GetType().Name}");
                return;
            }

            ReturnFunctionToPool(function);
            FlowLogger.Log(SystemLogType.Function, $"Function manually released! {function.GetType().Name}");
        }

        private void ReturnFunctionToPool(IFunctionBody functionBody)
        {
            Type functionType = functionBody.GetType();
            if (!_functionPool.TryGetValue(functionType, out Stack<IFunctionBody> pool))
            {
                pool = new Stack<IFunctionBody>();
                _functionPool[functionType] = pool;
            }

            functionBody.Dispose();
            pool.Push(functionBody);

            FlowLogger.LogWarning(SystemLogType.Function, "Function Returned to Pool! " + functionType.Name);
        }

        private IFunctionBody GetFunction(FunctionDataContainer functionDataContainer)
        {
            Type functionType = functionDataContainer.FunctionType;

            if (!_functionPool.TryGetValue(functionType, out Stack<IFunctionBody> pool))
            {
                pool = new Stack<IFunctionBody>();
                _functionPool[functionType] = pool;
            }

            IFunctionBody function = pool.Count > 0 ? pool.Pop() : (IFunctionBody)Activator.CreateInstance(functionType);

            if (function == null)
            {
                FlowLogger.LogWarning(SystemLogType.Function, "Function Created! " + functionType.Name);
            }

            return function;
        }
    }
}