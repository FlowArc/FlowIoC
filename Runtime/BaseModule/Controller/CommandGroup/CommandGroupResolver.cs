using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Controller.CommandGroup
{
    /// <summary>
    /// Manages the execution of a command group, which can contain commands and other command groups
    /// executed in sequence or in parallel.
    /// </summary>
    internal class CommandGroupResolver : ICommandGroupResolver
    {
        #region Properties and Events

        public Action<ICommandGroupResolver> GroupExecutionFinished { get; set; }

        internal bool IsHideLog { get; private set; }

        #endregion

        #region Private Fields

        private ICommandBinding _commandBinding;
        private CommandBinder _commandBinder;

        private object[] _signalParameters;
        private List<CommandStepVO> _steps;
        private Dictionary<Guid, CommandStepVO> _stepsDictionary;
        private Dictionary<ICommandBody, Guid> _retainedCommands;
        private int _executionIndex;
        private int _completionCount;
        private bool _isDisposed;

        #endregion

        #region Initialization and Cleanup

        public void Initialize(ICommandBinding commandBinding, CommandBinder commandBinder, params object[] signalParameters)
        {
            _commandBinder = commandBinder;
            _commandBinding = commandBinding;
            _signalParameters = signalParameters;

            _steps = _commandBinding.GetCommandSteps();
            _stepsDictionary = new Dictionary<Guid, CommandStepVO>(_steps.Count);
            _retainedCommands = new Dictionary<ICommandBody, Guid>();

            for (int i = 0; i < _steps.Count; i++)
                _stepsDictionary[_steps[i].Id] = _steps[i];

            _executionIndex = 0;
            _completionCount = 0;
            _isDisposed = false;
            IsHideLog = commandBinding.Key is ISignalBody signal && signal.HideCommandLog;

            CheckExecuteNextStep();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            GroupExecutionFinished = null;
            _steps = null;
            _stepsDictionary?.Clear();
            _retainedCommands?.Clear();
            _signalParameters = null;
            _executionIndex = 0;
            _completionCount = 0;
            IsHideLog = false;
        }

        #endregion

        #region Public API for Commands

        public void ReleaseCommand(ICommandBody command, params object[] commandParameters)
        {
            if (!command.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"Command must be retained to call manual RELEASE! Command: {command.GetType().Name}");
                return;
            }

            Guid stepId = Guid.Empty;
            bool wasTracked = _retainedCommands != null && _retainedCommands.TryGetValue(command, out stepId);
            if (wasTracked)
            {
                _retainedCommands.Remove(command);
            }

            _commandBinder.ReturnCommandToPool(command);

            if (!_isDisposed && wasTracked && _stepsDictionary != null && _stepsDictionary.TryGetValue(stepId, out var step))
            {
                HandleStepCompletion(step, commandParameters);
            }
        }

        public void StopCommand(ICommandBody command)
        {
            if (!command.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"Command must be retained to call STOP! Command: {command.GetType().Name}");
                return;
            }
            
            if (_isDisposed) return;

            Guid stepId = Guid.Empty;
            bool wasTracked = _retainedCommands != null && _retainedCommands.TryGetValue(command, out stepId);
            if (wasTracked)
            {
                _retainedCommands.Remove(command);
            }

            _commandBinder.ReturnCommandToPool(command);

            if (wasTracked && _stepsDictionary != null && _stepsDictionary.TryGetValue(stepId, out var step))
            {
                if (step.ExecutionType == CommandExecutionType.Parallel)
                {
                    _completionCount--;
                    if (_completionCount == 0)
                        CompleteGroupExecution();
                }
                else
                {
                    CompleteGroupExecution();
                }
            }
        }

        #endregion

        #region Execution Flow

        private void CheckExecuteNextStep(params object[] commandParameters)
        {
            if (_isDisposed) return;

            if (_executionIndex >= _steps.Count)
            {
                if (_completionCount == 0)
                    CompleteGroupExecution();
                return;
            }

            var step = _steps[_executionIndex++];

            if (step.GroupKey != null)
                ExecuteGroupStep(step);
            else if (step.CommandType != null)
                ExecuteCommandStep(step, commandParameters);

            if (step.ExecutionType == CommandExecutionType.Parallel)
                CheckExecuteNextStep(commandParameters);
        }

        private void HandleStepCompletion(CommandStepVO step, object[] commandParameters)
        {
            if (step.ExecutionType == CommandExecutionType.Sequence)
                CheckExecuteNextStep(commandParameters);

            _completionCount--;
            if (_completionCount == 0 )
                CompleteGroupExecution();
        }

        private void CompleteGroupExecution()
        {
            if (_isDisposed) return;

            GroupExecutionFinished?.Invoke(this);
            Dispose();
        }

        #endregion

        #region Step Executors

        private void ExecuteGroupStep(CommandStepVO step)
        {
            var groupBinding = _commandBinder.GetBinding(step.GroupKey);
            if (groupBinding == null)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"GroupKey '{step.GroupKey.Name}' could not be found in any context.");
                return;
            }

            var subGroup = _commandBinder.GetAvailableGroup();
            _completionCount++;

            Action<ICommandGroupResolver> onFinish = null;
            onFinish = (g) =>
            {
                g.GroupExecutionFinished -= onFinish;
                _commandBinder.ReturnGroupToPool(g);
                OnSubGroupFinished(step);
            };
            subGroup.GroupExecutionFinished += onFinish;

            if (!step.GroupKey.HideCommandLog)
                FlowLogger.Log(SystemLogType.CommandOperation, $"Command SubGroup is executed : '{step.GroupKey.Name}'.");

            var parametersToUse = step.SignalParameters?.Length > 0 ? step.SignalParameters : _signalParameters;
            subGroup.Initialize(groupBinding, _commandBinder, parametersToUse);
        }

        private void ExecuteCommandStep(CommandStepVO step, object[] commandParameters)
        {
            var command = _commandBinder.GetCommand(step.CommandType);
            command.CommandGroupResolver = this;
            
            _commandBinding.Context.InjectCommand(command, _signalParameters);

            _retainedCommands[command] = step.Id;

            _completionCount++;

            if (!_commandBinder.HasHideCommandLog(step.CommandType))
                FlowLogger.Log(SystemLogType.Command, $"[Command] Execute as {step.ExecutionType.ToString()} : {step.CommandType.Name}");
            
            InvokeCommandExecute(command, step.CommandParameters ?? commandParameters);

            if (!command.HasRetain)
            {
                _retainedCommands.Remove(command);
                _commandBinder.ReturnCommandToPool(command);
                HandleStepCompletion(step, null);
            }
        }

        private void InvokeCommandExecute(ICommandBody command, object[] parameters)
        {
            parameters ??= Array.Empty<object>();

            var type = command.GetType();
            var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance);//.Where(m => m.Name == "Execute").ToArray();

            // if (methods.Length == 0)
            // {
            //     FlowConsoleLogger.LogError(ConsoleLogType.Command, $"No public instance 'Execute' method found on '{type.Name}'.");
            //     return;
            // }
            //
            if (method == null)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"Multiple 'Execute' overloads found on '{type.Name}'. This invoker expects exactly one.");
                return;
            }
            //var method = methods[0];
            var paramInfos = method.GetParameters();

            bool isParamsArray = paramInfos.Length == 1 &&
                                 paramInfos[0].GetCustomAttribute<ParamArrayAttribute>() != null &&
                                 paramInfos[0].ParameterType == typeof(object[]);

            object[] invokeArgs = null;

            if (paramInfos.Length == 0)
            {
                invokeArgs = null;
            }
            else if (isParamsArray)
            {
                invokeArgs = new object[] {parameters};
            }
            else
            {
                if (paramInfos.Length != parameters.Length)
                {
                    var expected = string.Join(", ", paramInfos.Select(p => p.ParameterType.Name));
                    var actual = string.Join(", ", parameters.Select(p => p?.GetType().Name ?? "null"));
                    FlowLogger.LogError(SystemLogType.CommandOperation, $"Signature mismatch for Execute. Expected: ({expected}) | Provided: [{actual}]");
                    return;
                }

                for (int i = 0; i < paramInfos.Length; i++)
                {
                    var pi = paramInfos[i];
                    var arg = parameters[i];

                    if (arg == null)
                    {
                        if (pi.ParameterType.IsValueType && Nullable.GetUnderlyingType(pi.ParameterType) == null)
                        {
                            FlowLogger.LogError(SystemLogType.CommandOperation, $"Parameter {i} is null but '{pi.ParameterType.Name}' is non-nullable.");
                            return;
                        }
                    }
                    else if (!pi.ParameterType.IsInstanceOfType(arg))
                    {
                        FlowLogger.LogError(SystemLogType.CommandOperation, $"Type mismatch at parameter {i}. Expected '{pi.ParameterType.Name}', got '{arg.GetType().Name}'.");
                        return;
                    }
                }

                invokeArgs = parameters;
            }
            
            method.Invoke(command, invokeArgs);
        }

        #endregion

        #region Callbacks

        private void OnSubGroupFinished(CommandStepVO step)
        {
            if (_isDisposed) return;

            HandleStepCompletion(step, null);
        }

        #endregion
    }
}