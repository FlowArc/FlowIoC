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

        // A function's Execute is found once per type. It was looked up on every call, and a
        // Function is what a Command reaches for mid-Execute - so it runs as often as they do.
        private readonly Dictionary<Type, MethodInfo> _executeMethods = new();

        internal IContext Context;

        public FunctionProvider()
        {
            _functionDataContainerPool = new Dictionary<Type, Stack<FunctionDataContainer>>();
            _functionPool = new Dictionary<Type, Stack<IFunctionBody>>();
        }

        private MethodInfo GetExecuteMethod(Type functionType)
        {
            if (_executeMethods.TryGetValue(functionType, out MethodInfo cached))
                return cached;

            cached = functionType.GetMethod("Execute");
            _executeMethods[functionType] = cached;

            if (cached == null)
                FlowLogger.LogError(SystemLogType.Function, "No public Execute found on " + functionType.Name + ".");

            return cached;
        }

        private void ReturnDataContainerToPool(FunctionDataContainer functionDataContainer)
        {
            functionDataContainer.Dispose();

            Type containerType = functionDataContainer.GetType();

            if (!_functionDataContainerPool.TryGetValue(containerType, out Stack<FunctionDataContainer> pool))
            {
                pool = new Stack<FunctionDataContainer>();
                _functionDataContainerPool[containerType] = pool;
            }

            pool.Push(functionDataContainer);
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
            MethodInfo executeMethodInfo = GetExecuteMethod(functionDataContainer.FunctionType);

            Context.TryToInjectFunction(function);
            executeMethodInfo?.Invoke(function, functionDataContainer.ExecuteParameters);
            FlowLogger.Log(SystemLogType.Function, "Function Executed! " + function.GetType().Name);

            if (!function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            ReturnDataContainerToPool(functionDataContainer);
        }

        internal TReturnType ExecuteFunction<TReturnType>(FunctionDataContainer functionDataContainer)
        {
            IFunctionBody function = GetFunction(functionDataContainer);
            MethodInfo executeMethodInfo = GetExecuteMethod(functionDataContainer.FunctionType);

            Context.TryToInjectFunction(function);
            object result = executeMethodInfo?.Invoke(function, functionDataContainer.ExecuteParameters);
            FlowLogger.Log(SystemLogType.Function, "Function Executed! " + function.GetType().Name);

            ReturnDataContainerToPool(functionDataContainer);


            if (!function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            return (TReturnType) result;
        }

        internal IEnumerator ExecuteAsyncFunction(FunctionDataContainer functionDataContainer)
        {
            AsyncFunctionBody function = GetFunction(functionDataContainer) as AsyncFunctionBody;
            object functionCompletedCallback =
                functionDataContainer.GetType().GetProperty("FunctionCompletedCallback")?.GetValue(functionDataContainer);

            function?.GetType().GetProperty("FunctionCompletedCallback")?.SetValue(function, functionCompletedCallback);
            Context.TryToInjectFunction(function);
            yield return function?.Execute();
            FlowLogger.Log(SystemLogType.Function, "Function Executed! " + function?.GetType().Name);

            if (function != null && !function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            ReturnDataContainerToPool(functionDataContainer);
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

            FlowLogger.Log(SystemLogType.Function, "Function Returned to Pool! " + functionType.Name);
        }

        private IFunctionBody GetFunction(FunctionDataContainer functionDataContainer)
        {
            Type functionType = functionDataContainer.FunctionType;

            if (!_functionPool.TryGetValue(functionType, out Stack<IFunctionBody> pool))
            {
                pool = new Stack<IFunctionBody>();
                _functionPool[functionType] = pool;
            }

            if (pool.Count > 0)
                return pool.Pop();

            IFunctionBody function = (IFunctionBody) Activator.CreateInstance(functionType);
            FlowLogger.Log(SystemLogType.Function, "Function Created! " + functionType.Name);

            return function;
        }
    }
}