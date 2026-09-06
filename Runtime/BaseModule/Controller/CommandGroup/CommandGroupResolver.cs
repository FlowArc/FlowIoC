using System;
using System.Collections.Generic;
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

        // Retained commands, each against the index of the step that is waiting on it. The
        // dictionary belongs to the resolver rather than to a run, because the resolver is pooled
        // and allocating a fresh one per dispatch gave back half of what pooling saved.
        private readonly Dictionary<ICommandBody, int> _retainedCommands = new();

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
            _retainedCommands.Clear();

            _executionIndex = 0;
            _completionCount = 0;
            _isDisposed = false;
            IsHideLog = commandBinding.Key is ISignalBody signal && signal.HideCommandLog;

            CheckExecuteNextStep(null);
        }

        /// <summary>
        /// Ends the run and hands back what it still holds. A command retained when the group is
        /// stopped has no one left to release it, so the pool would never see it again - it is
        /// returned here instead, and the empty dictionary is what tells a late Release that its
        /// group is gone.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_retainedCommands.Count > 0)
            {
                foreach (KeyValuePair<ICommandBody, int> retained in _retainedCommands)
                    _commandBinder.ReturnCommandToPool(retained.Key);

                _retainedCommands.Clear();
            }

            GroupExecutionFinished = null;
            _steps = null;
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
                FlowLogger.LogError(SystemLogType.CommandOperation,
                    $"Command must be retained to call manual RELEASE! Command: {command.GetType().Name}");
                return;
            }

            if (!_retainedCommands.Remove(command, out int stepIndex))
            {
                // Nothing is waiting on this command any more - its group finished or was stopped,
                // and Dispose already handed it back. Returning it a second time would put the same
                // instance in the pool twice and hand it to two dispatches at once.
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"RELEASE arrived after the group ended. Command: {command.GetType().Name}");
                return;
            }

            _commandBinder.ReturnCommandToPool(command);

            if (!_isDisposed && stepIndex >= 0 && stepIndex < _steps.Count)
                HandleStepCompletion(_steps[stepIndex], commandParameters);
        }

        public void StopCommand(ICommandBody command)
        {
            if (!command.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"Command must be retained to call STOP! Command: {command.GetType().Name}");
                return;
            }

            if (_isDisposed) return;

            if (!_retainedCommands.Remove(command, out int stepIndex))
            {
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"STOP arrived after the group ended. Command: {command.GetType().Name}");
                return;
            }

            _commandBinder.ReturnCommandToPool(command);

            if (stepIndex < 0 || stepIndex >= _steps.Count)
                return;

            CommandStepVO step = _steps[stepIndex];

            if (step.ExecutionType == CommandExecutionType.Parallel)
            {
                _completionCount--;

                if (_completionCount == 0 && _executionIndex >= _steps.Count)
                    CompleteGroupExecution();
            }
            else
            {
                CompleteGroupExecution();
            }
        }

        #endregion

        #region Execution Flow

        private void CheckExecuteNextStep(object[] commandParameters)
        {
            if (_isDisposed) return;

            if (_executionIndex >= _steps.Count)
            {
                if (_completionCount == 0)
                    CompleteGroupExecution();
                return;
            }

            int stepIndex = _executionIndex++;
            CommandStepVO step = _steps[stepIndex];

            if (step.GroupKey != null)
                ExecuteGroupStep(step);
            else if (step.CommandType != null)
                ExecuteCommandStep(step, stepIndex, commandParameters);

            if (step.ExecutionType == CommandExecutionType.Parallel)
                CheckExecuteNextStep(commandParameters);
        }

        /// <summary>
        /// Marks one step done and decides whether the group is. Both halves matter: nothing is
        /// still running, and there is nothing left to start. A parallel step that finishes inside
        /// its own Execute used to satisfy the first on its own, so a group of parallel steps that
        /// were all synchronous ended after the first one and the rest were never reached.
        /// </summary>
        private void HandleStepCompletion(CommandStepVO step, object[] commandParameters)
        {
            if (step.ExecutionType == CommandExecutionType.Sequence)
                CheckExecuteNextStep(commandParameters);

            _completionCount--;

            if (_completionCount == 0 && _executionIndex >= _steps.Count)
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
            ICommandBinding groupBinding = _commandBinder.GetBinding(step.GroupKey);
            if (groupBinding == null)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation,
                    $"GroupKey '{step.GroupKey.Name}' could not be found in any context. The step is skipped.");

                // Counted and then closed, so the steps behind it still run. Returning here left
                // the step neither started nor finished: the sequence waited for a sub-group that
                // was never going to report, and the resolver never went back to the pool.
                _completionCount++;
                HandleStepCompletion(step, null);
                return;
            }

            ICommandGroupResolver subGroup = _commandBinder.GetAvailableGroup();
            _completionCount++;

            Action<ICommandGroupResolver> onFinish = null;
            onFinish = g =>
            {
                g.GroupExecutionFinished -= onFinish;
                _commandBinder.ReturnGroupToPool(g);
                OnSubGroupFinished(step);
            };
            subGroup.GroupExecutionFinished += onFinish;

            if (!step.GroupKey.HideCommandLog)
                FlowLogger.Log(SystemLogType.CommandOperation, $"Command SubGroup is executed : '{step.GroupKey.Name}'.");

            object[] parametersToUse = step.SignalParameters?.Length > 0 ? step.SignalParameters : _signalParameters;
            subGroup.Initialize(groupBinding, _commandBinder, parametersToUse);
        }

        private void ExecuteCommandStep(CommandStepVO step, int stepIndex, object[] commandParameters)
        {
            CommandBody command = _commandBinder.GetCommand(step.CommandType);

            // Both retain flags belong to this run and to no other. They used to survive in the
            // pooled instance, so a command that retained once was treated as retained on every
            // later run and its sequence stopped waiting for a Release nobody would send.
            command.BeginRun(this);

            _commandBinding.Context.InjectCommand(command, _signalParameters);

            _retainedCommands[command] = stepIndex;

            _completionCount++;

            if (!_commandBinder.HasHideCommandLog(step.CommandType))
                FlowLogger.Log(SystemLogType.Command, $"[Command] Execute as {step.ExecutionType.ToString()} : {step.CommandType.Name}");

            command.InvokeExecute(step.CommandParameters ?? commandParameters ?? Array.Empty<object>());

            if (!command.HasRetain)
            {
                _retainedCommands.Remove(command);
                _commandBinder.ReturnCommandToPool(command);
                HandleStepCompletion(step, null);
            }
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