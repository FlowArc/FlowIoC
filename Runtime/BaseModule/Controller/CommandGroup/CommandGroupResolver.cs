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

        // Retained commands, each against the step that is waiting on it. The step itself rather
        // than its index: an index has to be read back against a list, which is a question that can
        // be answered wrongly, and a Stop that could not answer it used to return having already
        // pooled the command and without ever reporting - a group left waiting on a step nobody
        // would finish. A step reached through the dictionary is the one the command was started
        // for, and there is nothing left to get wrong.
        //
        // The dictionary belongs to the resolver rather than to a run, because the resolver is
        // pooled and allocating a fresh one per dispatch gave back half of what pooling saved.
        private readonly Dictionary<ICommandBody, CommandStepVO> _retainedCommands = new();

        private int _executionIndex;
        private int _completionCount;
        private bool _isDisposed;

        // Whether a driver is already running, and whether something asked it to carry on while it
        // was. A completion that lands mid-step sets the request and returns rather than starting
        // the next step from inside the one before it, which is what kept a whole sequence's frames
        // on the stack until it had finished.
        private bool _isPumping;
        private bool _advanceRequested;

        // A sequence step that stopped takes the steps behind it with it. The driver reads this and
        // starts nothing more; the group is ended by the driver's exit rather than from inside the
        // Stop, so no frame of this run is left on the stack once the resolver is back in the pool.
        private bool _isStopped;

        // What the last completion released, waiting for the steps the driver starts because of it.
        // It is read once per turn of the loop rather than once per step: the recursion handed the
        // same array to every parallel step it started in one go, and consuming it per step would
        // feed only the first of two parallel steps sitting behind a sequence step that released.
        private object[] _pendingParameters;

        // The resolver this one is a sub group of, and the run that parent was on when it started
        // this step. A sub group used to report through a closure hung on GroupExecutionFinished,
        // which cost two allocations per group step - the delegate, and the display class the
        // captured run id forced. The resolver is pooled, so holding the two here costs nothing,
        // and the parent is told directly. Both are null and 0 for a dispatch's own group.
        //
        // Set by SetParent before Initialize runs, and therefore not cleared by Initialize - which
        // is safe because Dispose clears them and nothing reaches the pool without being disposed.
        private CommandGroupResolver _parent;
        private int _parentRunId;

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

        // Where the sequence this run belongs to was declared. Carried for the same reason the
        // flow is: a chain that came back a frame later has to re-enter both.
        private string _declarationFile;
        private int _declarationLine;

        private static readonly FlowFrameworkOrigin Origin = new();

        #endregion

        #region Initialization and Cleanup

        /// <summary>
        /// Names the resolver this one is a sub group of, and the run that parent was on. It must be
        /// called before <see cref="Initialize"/>, because a sub group of synchronous steps runs to
        /// the end inside that call and reports before it returns.
        /// </summary>
        internal void SetParent(CommandGroupResolver parent, int parentRunId)
        {
            _parent = parent;
            _parentRunId = parentRunId;
        }

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
            _isStopped = false;
            _isPumping = false;
            _advanceRequested = false;
            _pendingParameters = null;
            _runId++;
            IsHideLog = commandBinding.Key is ISignalBody signal && signal.HideCommandLog;

            // The flow is started by whoever starts the group - CommandBinder for a dispatch, and
            // ExecuteGroupStep for a sub group - so that the line announcing the group belongs to
            // the same flow as the commands under it. This run only remembers which flow that is,
            // because ReleaseCommand and StopCommand have to re-enter it a frame later.
            FlowLogger.CaptureCurrentFlow(ref _flowId, ref _parentFlowId);
            FlowLogger.CaptureCurrentDeclaration(ref _declarationFile, ref _declarationLine);

            Pump();
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
            _parent = null;
            _parentRunId = 0;
            _steps = null;
            _signalParameters = null;
            _executionIndex = 0;
            _completionCount = 0;
            _isStopped = false;
            _advanceRequested = false;
            _pendingParameters = null;
            _flowId = 0;
            _parentFlowId = 0;
            _declarationFile = null;
            _declarationLine = 0;

            // IsHideLog is deliberately left where it is. The binder reads it on the way to the
            // pool, which is after this now, and Initialize sets it for the next run anyway.

            // _isPumping is not cleared here either, and for a sharper reason: the driver's own
            // finally owns it. Clearing it from a Dispose that landed mid-run would let the next
            // re-entrant call start a second driver on the same instance, which is the one thing
            // the flag exists to prevent. Initialize sets it false for the next run.
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

            string previousFile = null;
            int previousLine = 0;
            FlowLogger.EnterDeclaration(_declarationFile, _declarationLine, ref previousFile, ref previousLine);

            try
            {
                ReleaseCommandInFlow(command, commandParameters);
            }
            finally
            {
                FlowLogger.ExitDeclaration(previousFile, previousLine);
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

            // The step index the dictionary carries is not read here: a hit means this run is still
            // waiting on the command, and the driver knows which step that is.
            if (!_retainedCommands.Remove(command, out _))
            {
                // Nothing is waiting on this command any more - its group finished or was stopped,
                // and Dispose already handed it back. Returning it a second time would put the same
                // instance in the pool twice and hand it to two dispatches at once.
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"RELEASE arrived after the group ended. Command: {command.GetType().Name}", command.GetType());
                return;
            }

            _commandBinder.ReturnCommandToPool(command);

            HandleStepCompletion(commandParameters);
        }

        [UnityEngine.HideInCallstack]
        public void StopCommand(ICommandBody command)
        {
            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(_flowId, _parentFlowId, ref previousFlowId, ref previousParentFlowId);

            string previousFile = null;
            int previousLine = 0;
            FlowLogger.EnterDeclaration(_declarationFile, _declarationLine, ref previousFile, ref previousLine);

            try
            {
                StopCommandInFlow(command);
            }
            finally
            {
                FlowLogger.ExitDeclaration(previousFile, previousLine);
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

            if (!_retainedCommands.Remove(command, out CommandStepVO step))
            {
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    $"STOP arrived after the group ended. Command: {command.GetType().Name}", command.GetType());
                return;
            }

            _commandBinder.ReturnCommandToPool(command);

            if (step.ExecutionType == CommandExecutionType.Parallel)
                _completionCount--;
            else
                // A sequence step that stops takes the steps behind it with it.
                _isStopped = true;

            // The group is ended by the driver's exit rather than from here, so a Stop called from
            // inside a command's own Execute does not hand the resolver back to the pool while that
            // Execute is still on the stack.
            Pump();
        }

        #endregion

        #region Execution Flow

        /// <summary>
        /// The one place a step is started from. Re-entered - by a command that finished inside its
        /// own Execute, by a sub group that ran to the end on this stack - it records that there is
        /// more to do and returns, and the driver already running takes it on its next turn.
        ///
        /// That is the whole point of the loop. Driving the steps by recursion left every step's
        /// frames on the stack until the sequence had finished, so the group could end and the
        /// resolver go back to the pool while frames of that same run were still waiting to resume:
        /// a command that dispatched a signal took the instance out again, Initialize reset the run,
        /// and those frames woke up on somebody else's steps. Here nothing of a run is left on the
        /// stack once work of another run can begin, so the case cannot arise rather than being
        /// caught after it has.
        /// </summary>
        private void Pump()
        {
            if (_isPumping)
            {
                _advanceRequested = true;
                return;
            }

            _isPumping = true;

            try
            {
                do
                {
                    _advanceRequested = false;

                    // Read once per turn rather than once per step, because the recursion handed the
                    // same array to every parallel step it started in one go. Consuming it per step
                    // would feed only the first of two parallel steps behind a sequence step that
                    // released a value.
                    object[] commandParameters = _pendingParameters;
                    _pendingParameters = null;

                    while (!_isDisposed && !_isStopped && _executionIndex < _steps.Count && CanStartNextStep())
                        StartStep(_executionIndex++, commandParameters);
                } while (_advanceRequested && !_isDisposed);
            }
            finally
            {
                _isPumping = false;
            }

            if (_isDisposed) return;

            if (_isStopped || (_completionCount == 0 && _executionIndex >= _steps.Count))
                CompleteGroupExecution();
        }

        /// <summary>
        /// Whether the step waiting at <see cref="_executionIndex"/> may start now. A sequence step
        /// holds the ones behind it until it finishes; a parallel step holds nothing, so whatever
        /// follows one starts beside it - which is how a parallel step in front of a sequence step
        /// gets both of them running.
        /// </summary>
        private bool CanStartNextStep()
        {
            if (_executionIndex == 0 || _completionCount == 0)
                return true;

            return _steps[_executionIndex - 1].ExecutionType == CommandExecutionType.Parallel;
        }

        private void StartStep(int stepIndex, object[] commandParameters)
        {
            CommandStepVO step = _steps[stepIndex];

            if (step.GroupKey != null)
                ExecuteGroupStep(step);
            else if (step.CommandType != null)
                ExecuteCommandStep(step, commandParameters);
        }

        /// <summary>
        /// Marks one step done and asks the driver to carry on. It does not decide whether the group
        /// is finished, and that is deliberate: only the driver's exit does, once nothing is running
        /// and there is nothing left to start. Deciding here is what used to end a group of parallel
        /// steps after the first one, because a step that finished inside its own Execute satisfied
        /// "nothing is running" on its own while the rest had not been reached yet.
        /// </summary>
        private void HandleStepCompletion(object[] commandParameters)
        {
            if (_isDisposed) return;

            _completionCount--;

            if (commandParameters != null)
                _pendingParameters = commandParameters;

            Pump();
        }

        /// <summary>
        /// Ends the run before it says so. Whoever is told puts the resolver back in its pool, and a
        /// step of whatever runs next can take it straight out again - so anything this method did
        /// after that would be done to somebody else's run. Everything it needs is read into locals
        /// first and the run is disposed, which leaves nothing to read afterwards.
        ///
        /// Who is told depends on what started the group, and the two are exclusive: a dispatch's
        /// own group has the listener the binder subscribed and no parent, and a sub group has a
        /// parent and no listener.
        /// </summary>
        private void CompleteGroupExecution()
        {
            if (_isDisposed) return;

            Action<ICommandGroupResolver> finished = GroupExecutionFinished;
            CommandGroupResolver parent = _parent;
            int parentRunId = _parentRunId;
            CommandBinder binder = _commandBinder;

            Dispose();

            if (parent == null)
            {
                finished?.Invoke(this);
                return;
            }

            // Pooled before the parent is told, so the instance is there for whatever the parent
            // starts next - and through the binder this run belonged to rather than through the one
            // the parent may since have been re-initialised with.
            binder.ReturnGroupToPool(this);
            parent.OnSubGroupFinished(parentRunId);
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
                HandleStepCompletion(null);
                return;
            }

            CommandGroupResolver subGroup = _commandBinder.GetAvailableGroup();
            _completionCount++;

            // The sub group is told who to report to and which run of this resolver it belongs to,
            // rather than being subscribed to with a closure that captured the same two things. It
            // has to be told before Initialize, which runs a group of synchronous steps to the end
            // and reports before it returns.
            subGroup.SetParent(this, _runId);

            // A sub group is a branch of this flow rather than part of it, so it takes a flow of
            // its own with this one as its parent - the same shape a nested dispatch gets.
            int subFlowId = 0, subParentFlowId = 0;
            FlowLogger.NextFlowId(ref subFlowId, ref subParentFlowId);

            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(subFlowId, subParentFlowId, ref previousFlowId, ref previousParentFlowId);
            try
            {
                if (!step.GroupKey.HideCommandLog)
                    FlowLogger.LogPlumbing(SystemLogType.CommandOperation, "'", step.GroupKey.Name, "' opened a sub group");

                object[] parametersToUse = step.SignalParameters?.Length > 0 ? step.SignalParameters : _signalParameters;
                subGroup.Initialize(groupBinding, _commandBinder, parametersToUse);
            }
            finally
            {
                FlowLogger.ExitFlow(previousFlowId, previousParentFlowId);
            }
        }

        private void ExecuteCommandStep(CommandStepVO step, object[] commandParameters)
        {
            CommandBody command = _commandBinder.GetCommand(step.CommandType);

            // Both retain flags belong to this run and to no other. They used to survive in the
            // pooled instance, so a command that retained once was treated as retained on every
            // later run and its sequence stopped waiting for a Release nobody would send.
            command.BeginRun(this);
            int runToken = command.RunToken;

            _commandBinding.Context.InjectCommand(command, _signalParameters);

            _retainedCommands[command] = step;

            _completionCount++;

            // Asked before the message is built rather than inside the call: an enum's name is a
            // lookup and an allocation, and this line runs for every command of every dispatch.
            if (FlowLogger.IsEnabled && !_commandBinder.HasHideCommandLog(step.CommandType))
                // Two different questions, and they have two different answers.
                //
                // Which channel: whose sequence this step is in. A step bound by one of the
                // framework's own Contexts - registering a screen, releasing a group of assets -
                // is the framework running itself and goes on the plumbing channel. The Command
                // channel is the game's own flow, read top to bottom.
                //
                // Whether it opens anything: whose class is running. A game binds
                // DispatchSignalCommand as a step of its own, and that step belongs in its flow -
                // but the class is the framework's, and taking a reader there tells them nothing.
                if (Origin.IsFrameworkType(_commandBinder?.Context?.GetType()))
                {
                    FlowLogger.LogPlumbing(SystemLogType.CommandOperation,
                        step.CommandType.Name, " executed as ", step.ExecutionType.ToString());
                }
                else
                {
                    FlowLogger.LogAbout(SystemLogType.Command,
                        Origin.IsFrameworkType(step.CommandType) ? null : step.CommandType,
                        step.CommandType.Name, " executed as ", step.ExecutionType.ToString());
                }

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
                HandleStepCompletion(null);
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// A sub group of this resolver has finished and already put itself back in the pool. The
        /// run id it carries is the one this resolver was on when the step started: by now this
        /// resolver may itself have finished, gone to the pool and been taken out for something
        /// else, in which case the step being reported no longer exists.
        /// </summary>
        internal void OnSubGroupFinished(int runId)
        {
            if (_isDisposed || runId != _runId) return;

            HandleStepCompletion(null);
        }

        #endregion
    }
}