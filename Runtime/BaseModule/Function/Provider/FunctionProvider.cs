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
using FlowIoC.BaseModule.Pooling;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Function.Provider
{
    [HideInModelViewer]
    public class FunctionProvider : IFunctionProvider
    {
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }

        private readonly TypePool<FunctionDataContainer> _functionDataContainerPool = new();
        private readonly TypePool<IFunctionBody> _functionPool = new();

        // Only a function written straight on FunctionBody - one with no typed Execute for the
        // provider to reach - is still called by name. Its Execute is found once per type.
        private readonly Dictionary<Type, MethodInfo> _executeMethods = new();

        internal IContext Context;

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
            _functionDataContainerPool.Return(functionDataContainer.GetType(), functionDataContainer);
        }

        private FunctionDataContainer GetFunctionDataContainer<TDataContainerType>() where TDataContainerType : FunctionDataContainer, new()
        {
            Type dataContainerType = typeof(TDataContainerType);

            FunctionDataContainer availableFunctionDataContainer =
                _functionDataContainerPool.TryTake(dataContainerType, out FunctionDataContainer parked)
                    ? parked
                    : new TDataContainerType();

            availableFunctionDataContainer.FunctionProvider = this;
            if (availableFunctionDataContainer is AsyncFunctionDataContainerBase asyncDataContainer)
            {
                asyncDataContainer.CoroutineProvider = _coroutineProvider;
            }

            return availableFunctionDataContainer;
        }

        public IFunctionDataContainer Call<TFunctionType>() where TFunctionType : IFunctionBody
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<FunctionDataContainer>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer;
        }

        public IAsyncFunctionDataContainer CallAsync<TFunctionType>() where TFunctionType : IAsyncFunction
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<AsyncFunctionDataContainer>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer as IAsyncFunctionDataContainer;
        }

        public IAsyncFunctionDataContainer<TParam1> CallAsync<TFunctionType, TParam1>() where TFunctionType : IAsyncFunction<TParam1>
        {
            Type functionType = typeof(TFunctionType);
            FunctionDataContainer functionDataContainer = GetFunctionDataContainer<AsyncFunctionDataContainer<TParam1>>();
            functionDataContainer.SetFunctionType(functionType);

            return functionDataContainer as IAsyncFunctionDataContainer<TParam1>;
        }

        internal void ExecuteFunction(FunctionDataContainer functionDataContainer)
        {
            IFunctionBody function = GetFunction(functionDataContainer);
            int runToken = RunTokenOf(function);

            Context.TryToInjectFunction(function);
            Invoke(function, functionDataContainer.ExecuteParameters, out _);
            FlowLogger.Log(SystemLogType.Function, "Function Executed! ", function.GetType().Name);

            if (IsStillTheSameRun(function, runToken) && !function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            ReturnDataContainerToPool(functionDataContainer);
        }

        internal TReturnType ExecuteFunction<TReturnType>(FunctionDataContainer functionDataContainer)
        {
            IFunctionBody function = GetFunction(functionDataContainer);
            int runToken = RunTokenOf(function);

            Context.TryToInjectFunction(function);
            Invoke(function, functionDataContainer.ExecuteParameters, out object result);
            FlowLogger.Log(SystemLogType.Function, "Function Executed! ", function.GetType().Name);

            ReturnDataContainerToPool(functionDataContainer);

            if (IsStillTheSameRun(function, runToken) && !function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            // A mismatch the function reported hands back nothing, and nothing has to unbox into a
            // value type without throwing on top of the report.
            return result is TReturnType typed ? typed : default;
        }

        internal IEnumerator ExecuteAsyncFunction(FunctionDataContainer functionDataContainer)
        {
            AsyncFunctionBody function = GetFunction(functionDataContainer) as AsyncFunctionBody;
            if (function == null)
            {
                FlowLogger.LogError(SystemLogType.Function,
                    functionDataContainer.FunctionType?.Name + " is not an AsyncFunction, so it cannot run as one.");
                ReturnDataContainerToPool(functionDataContainer);
                yield break;
            }

            int runToken = RunTokenOf(function);

            (functionDataContainer as AsyncFunctionDataContainerBase)?.ApplyCallback(function);
            Context.TryToInjectFunction(function);
            yield return function.Execute();
            FlowLogger.Log(SystemLogType.Function, "Function Executed! ", function.GetType().Name);

            if (IsStillTheSameRun(function, runToken) && !function.HasRetain)
            {
                ReturnFunctionToPool(function);
            }

            ReturnDataContainerToPool(functionDataContainer);
        }

        /// <summary>
        /// Runs the function's Execute. A function of one of the shipped arities calls its own
        /// typed Execute; anything else is called by name, the way every function used to be.
        /// </summary>
        private void Invoke(IFunctionBody function, object[] parameters, out object result)
        {
            if (function is FunctionBody body && body.TryInvokeExecute(parameters ?? Array.Empty<object>(), out result))
                return;

            MethodInfo executeMethodInfo = GetExecuteMethod(function.GetType());
            result = executeMethodInfo?.Invoke(function, parameters);
        }

        public void ReleaseFunctionManually(IFunctionBody function)
        {
            if (!function.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.Function, $"Function must be retained to call manual RELEASE! Function: {function.GetType().Name}");
                return;
            }

            ReturnFunctionToPool(function);
            FlowLogger.Log(SystemLogType.Function, "Function manually released! ", function.GetType().Name);
        }

        private void ReturnFunctionToPool(IFunctionBody functionBody)
        {
            Type functionType = functionBody.GetType();

            functionBody.Dispose();
            _functionPool.Return(functionType, functionBody);

            FlowLogger.Log(SystemLogType.Function, "Function Returned to Pool! ", functionType.Name);
        }

        /// <summary>
        /// A function ready for one execution. Both retain flags are cleared here rather than on
        /// the way back to the pool, so an instance that released itself mid-Execute is still
        /// carrying the answer its own run needs when that run is judged.
        /// </summary>
        private IFunctionBody GetFunction(FunctionDataContainer functionDataContainer)
        {
            Type functionType = functionDataContainer.FunctionType;

            if (!_functionPool.TryTake(functionType, out IFunctionBody function))
            {
                function = (IFunctionBody) Activator.CreateInstance(functionType);
                FlowLogger.Log(SystemLogType.Function, "Function Created! ", functionType.Name);
            }

            (function as FunctionBody)?.BeginRun();

            return function;
        }

        /// <summary>
        /// Whether the run that took this instance out is still the run that holds it. A function
        /// that retained and released inside its own Execute is back in the pool while that Execute
        /// is still on the stack, and a nested call of the same type takes the very same instance
        /// out again - in which case the flags belong to that run and pooling the instance here
        /// would hand a running function to a second caller.
        /// </summary>
        private static bool IsStillTheSameRun(IFunctionBody function, int runToken) =>
            function is not FunctionBody body || body.RunToken == runToken;

        private static int RunTokenOf(IFunctionBody function) =>
            function is FunctionBody body ? body.RunToken : 0;
    }
}