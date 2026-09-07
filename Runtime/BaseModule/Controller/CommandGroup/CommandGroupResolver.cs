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

        // Which run the resolver is on. The resolver is pooled, so a frame of a finished run can
        // still be on the stack when a nested dispatch takes the same instance back out of the pool
        // and starts a new run on it. Every place that carries on after calling out reads this and
        // stops if it has moved: the frame belongs to a run that is over.
        private int _runId;

        // The flow this run belongs to, and the flow it was dispatched from. Everything the
        // console records while this resolver is running is tagged with them, which is what lets
        // the window draw a dispatch as a tree instead of a run of unrelated lines. Both stay 0
        // in a player build, because the calls that fill them carry [Conditional("ENABLE_LOG")]
        // and are removed by the compiler.
        private int _flowId;
        private int _parentFlowId;

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
            _runId++;
            IsHideLog = commandBinding.Key is ISignalBody signal && signal.HideCommandLog;

            // The flow is started by whoever starts the group - CommandBinder for a dispatch, and
            // ExecuteGroupStep for a sub group - so that the line announcing the group belongs to
            // the same flow as the commands under it. This run only remembers which flow that is,
            // because ReleaseCommand and StopCommand have to re-enter it a frame later.
            FlowLogger.CaptureCurrentFlow(ref _flowId, ref _parentFlowId);

            CheckExecuteNextStep(null);
        }

        /// <summary>
        /// Ends the run and lets go of what it still holds. A command retained when the group ends
        /// is dropped rather than pooled: whatever it was waiting on is still going, still holds the
        /// instance, and will call Release on it. Handing that instance to the next dispatch would
        /// make the late Release finish a step of somebody else's run - a bug worth more than the
        /// one pooled instance that is lost by letting it go. The empty dictionary is what tells the
        /// late Release its group is gone.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _retainedCommands.Clear();

            GroupExecutionFinished = null;
            _steps = null;
            _signalParameters = null;
            _executionIndex = 0;
            _completionCount = 0;
            _flowId = 0;
            _parentFlowId = 0;

            // IsHideLog is deliberately left where it is. The binder reads it on the way to the
            // pool, which is after this now, and Initialize sets it for the next run anyway.
        }

        #endregion

        #region Public API for Commands

        [UnityEngine.HideInCallstack]
        public void ReleaseCommand(ICommandBody command, params object[] commandParameters)
        {
            // The door a chain comes back through a frame later, so it re-enters the flow the
            // dispatch started. Wrapping the steps instead would leave everything after a Retain
            // outside the tree.
            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(_flowId, _parentFlowId, ref previousFlowId, ref previousParentFlowId);
            try
            {
                ReleaseCommandInFlow(command, commandParameters);
            }
            finally
            {
                FlowLogger.ExitFlow(previousFlowId, previousParentFlowId);
            }
        }

        [UnityEngine.HideInCallstack]
        private void ReleaseCommandInFlow(ICommandBody command, object[] commandParameters)
        {
            if (!command.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation,
                    $"Command must be retained to call manual RELEASE! Command: {command.GetType().Name}", command.GetType());
                return;
            }

            if (!_retainedCommands.Remove(command, out int stepIndex))
            {
                // Nothing is waiting on this command any more - its group finished or was stopped,
                // and Dispose already handed it back. Returning it a second time would put the same
                // instance in the pool twice and hand it to two dispatches at once.
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"RELEASE arrived after the group ended. Command: {command.GetType().Name}", command.GetType());
                return;
            }

            _commandBinder.ReturnCommandToPool(command);

            if (!_isDisposed && stepIndex >= 0 && stepIndex < _steps.Count)
                HandleStepCompletion(_steps[stepIndex], commandParameters);
        }

        [UnityEngine.HideInCallstack]
        public void StopCommand(ICommandBody command)
        {
            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(_flowId, _parentFlowId, ref previousFlowId, ref previousParentFlowId);
            try
            {
                StopCommandInFlow(command);
            }
            finally
            {
                FlowLogger.ExitFlow(previousFlowId, previousParentFlowId);
            }
        }

        [UnityEngine.HideInCallstack]
        private void StopCommandInFlow(ICommandBody command)
        {
            if (!command.IsRetain)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation, $"Command must be retained to call STOP! Command: {command.GetType().Name}",
                    command.GetType());
                return;
            }

            if (_isDisposed) return;

            if (!_retainedCommands.Remove(command, out int stepIndex))
            {
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"STOP arrived after the group ended. Command: {command.GetType().Name}", command.GetType());
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

            int runId = _runId;

            int stepIndex = _executionIndex++;
            CommandStepVO step = _steps[stepIndex];

            if (step.GroupKey != null)
                ExecuteGroupStep(step);
            else if (step.CommandType != null)
                ExecuteCommandStep(step, stepIndex, commandParameters);

            // The step may have finished the group and sent the resolver back to the pool, and a
            // command it ran may have dispatched a signal that took it out again. Starting the next
            // parallel step then would start it on somebody else's run.
            if (step.ExecutionType == CommandExecutionType.Parallel && runId == _runId)
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
            int runId = _runId;

            if (step.ExecutionType == CommandExecutionType.Sequence)
                CheckExecuteNextStep(commandParameters);

            if (runId != _runId || _isDisposed) return;

            _completionCount--;

            if (_completionCount == 0 && _executionIndex >= _steps.Count)
                CompleteGroupExecution();
        }

        /// <summary>
        /// Ends the run before it says so. The listener is the binder putting the resolver back in
        /// its pool, and a step of whatever runs next can take it straight out again - so anything
        /// this method did after the callback would be done to somebody else's run. Disposing first
        /// means there is nothing left to do after it.
        /// </summary>
        private void CompleteGroupExecution()
        {
            if (_isDisposed) return;

            Action<ICommandGroupResolver> finished = GroupExecutionFinished;
            Dispose();
            finished?.Invoke(this);
        }

        #endregion

        #region Step Executors

        private void ExecuteGroupStep(CommandStepVO step)
        {
            ICommandBinding groupBinding = _commandBinder.GetBinding(step.GroupKey);
            if (groupBinding == null)
            {
                string here = _commandBinder.Context == null ? "this context" : _commandBinder.Context.GetType().Name;

                FlowLogger.LogError(SystemLogType.CommandOperation,
                    $"GroupKey '{step.GroupKey.Name}' is not bound in {here}, and a group is looked up in the " +
                    "context that binds it and nowhere else. The step is skipped.");

                // Counted and then closed, so the steps behind it still run. Returning here left
                // the step neither started nor finished: the sequence waited for a sub-group that
                // was never going to report, and the resolver never went back to the pool.
                _completionCount++;
                HandleStepCompletion(step, null);
                return;
            }

            ICommandGroupResolver subGroup = _commandBinder.GetAvailableGroup();
            _completionCount++;

            // The run this step belongs to. The sub group reports back through a closure, and by
            // then this resolver may itself have finished, gone to the pool and been taken out for
            // something else - in which case the step it is reporting no longer exists.
            int runId = _runId;

            Action<ICommandGroupResolver> onFinish = null;
            onFinish = g =>
            {
                g.GroupExecutionFinished -= onFinish;
                _commandBinder.ReturnGroupToPool(g);

                if (runId == _runId)
                    OnSubGroupFinished(step);
            };
            subGroup.GroupExecutionFinished += onFinish;

            // A sub group is a branch of this flow rather than part of it, so it takes a flow of
            // its own with this one as its parent - the same shape a nested dispatch gets.
            int subFlowId = 0, subParentFlowId = 0;
            FlowLogger.NextFlowId(ref subFlowId, ref subParentFlowId);

            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(subFlowId, subParentFlowId, ref previousFlowId, ref previousParentFlowId);
            try
            {
                if (!step.GroupKey.HideCommandLog)
                    FlowLogger.Log(SystemLogType.CommandOperation, "Command SubGroup is executed : '", step.GroupKey.Name, "'.");

                object[] parametersToUse = step.SignalParameters?.Length > 0 ? step.SignalParameters : _signalParameters;
                subGroup.Initialize(groupBinding, _commandBinder, parametersToUse);
            }
            finally
            {
                FlowLogger.ExitFlow(previousFlowId, previousParentFlowId);
            }
        }

        private void ExecuteCommandStep(CommandStepVO step, int stepIndex, object[] commandParameters)
        {
            CommandBody command = _commandBinder.GetCommand(step.CommandType);

            // Both retain flags belong to this run and to no other. They used to survive in the
            // pooled instance, so a command that retained once was treated as retained on every
            // later run and its sequence stopped waiting for a Release nobody would send.
            command.BeginRun(this);
            int runToken = command.RunToken;

            _commandBinding.Context.InjectCommand(command, _signalParameters);

            _retainedCommands[command] = stepIndex;

            _completionCount++;

            // Asked before the message is built rather than inside the call: an enum's name is a
            // lookup and an allocation, and this line runs for every command of every dispatch.
            if (FlowLogger.IsEnabled && !_commandBinder.HasHideCommandLog(step.CommandType))
                FlowLogger.Log(SystemLogType.Command, "[Command] Execute as ", step.ExecutionType.ToString(), " : ", step.CommandType.Name);

            command.InvokeExecute(step.CommandParameters ?? commandParameters ?? Array.Empty<object>());

            // A command that retained and released inside that Execute is already back in the pool,
            // and a later step of the same type may have taken it out again - in which case the
            // flags below belong to that run, not this one, and returning the instance would put a
            // running command in the pool for a second dispatch to pick up.
            if (command.RunToken != runToken)
                return;

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