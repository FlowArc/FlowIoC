using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Bind.Binders;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.CommandGroup;
using FlowIoC.BaseModule.Pooling;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Controller.Binders
{
    [HideInModelViewer]
    public class CommandBinder : Binder<CommandBinding>, ICommandBinder
    {
        private readonly Dictionary<Type, bool> _hideCommandLogCache = new();
        private readonly TypePool<CommandBody> _commandPool = new();
        private readonly Stack<ICommandGroupResolver> _commandGroupPool = new();

        /// <summary>
        /// The signals whose command callback this binder put there. It is what UnBind gives back,
        /// and a list because a context binds a handful of signals and never searches this.
        /// </summary>
        private readonly List<ISignalBody> _ownedSignals = new();

        /// <summary>
        /// One delegate for the life of the binder. Writing the method group at the subscription
        /// built a new one on every dispatch, which is the kind of allocation a signal that fires
        /// each frame notices.
        /// </summary>
        private readonly Action<ICommandGroupResolver> _returnGroupToPool;

        internal IContext Context;

        public CommandBinder()
        {
            _returnGroupToPool = ReturnGroupToPool;
        }

        /// <summary>
        /// A signal carries one command callback, so the first Context to bind it owns it. A second
        /// Context binding the same signal used to overwrite that callback without a word, and the
        /// first Context's commands then never ran. The binding is still handed back so the chain
        /// that follows reads normally, but the signal keeps the owner it already had.
        /// </summary>
        public virtual ICommandBinding Bind<TSignal>(TSignal key)
            where TSignal : ISignalBody
        {
            // The same signal bound twice in one Context. The base binder answers that with null,
            // which the chain's first ToSequence then dereferenced - so a mistake in a Context was
            // reported as a null reference somewhere in the framework. It is named here instead.
            if (GetBinding(key) != null)
            {
                FlowLogger.LogError(SystemLogType.CommandOperation,
                    "<b><color=#FF6666>► Signal is bound twice in the same context!</color></b>\n" +
                    "<b><color=#FF6666>► Signal:</color><color=#FFEFD5> " + key.Name + "</color></b>\n" +
                    "<b><color=#FF6666>► Context:</color><color=#FFEFD5> " +
                    (Context == null ? "this context" : Context.GetType().Name) + "</color></b>\n" +
                    "<b><color=#FF6666>► Result:</color><color=#FFEFD5> only the first chain runs. " +
                    "Put the steps in that one sequence rather than binding the signal again.</color></b>",
                    "Signal '" + key.Name + "' is bound twice in the same context, so only the first chain runs.");

                return DiscardedBinding(key);
            }

            Action<ISignalBody, object[]> boundElsewhere = key.InternalCallback;

            if (boundElsewhere != null && !ReferenceEquals(boundElsewhere.Target, this))
            {
                LogSignalAlreadyOwned(key, boundElsewhere);
            }
            else
            {
                if (boundElsewhere == null)
                    _ownedSignals.Add(key);

                key.InternalCallback = InitializeGroupWithSignal;
            }

            CommandBinding binding = base.Bind(key);
            binding?.SetContext(Context);
            return binding;
        }

        /// <summary>
        /// Ownership goes back with the binding. This binder put the callback on the signal, so
        /// tearing the binder down has to take it off again - a signal still pointing at a binder
        /// with no bindings left refuses the next Context that binds it, for a run that is already
        /// over. Reloading a scene is exactly that: the contexts are rebuilt while the signal
        /// holder, which lives in the cross-context binder, is the same instance it was.
        /// </summary>
        public override void UnBind(object key)
        {
            if (key is ISignalBody signal)
                ReleaseSignal(signal);

            base.UnBind(key);
        }

        public override void UnBindAll()
        {
            for (int i = 0; i < _ownedSignals.Count; i++)
                ReleaseOwnership(_ownedSignals[i]);

            _ownedSignals.Clear();
            base.UnBindAll();
        }

        private void ReleaseSignal(ISignalBody signal)
        {
            if (!_ownedSignals.Remove(signal))
                return;

            ReleaseOwnership(signal);
        }

        /// <summary>
        /// Only the callback this binder put there is taken off. A signal another binder has since
        /// taken over is left alone, so an unbind never silences somebody else's commands.
        /// </summary>
        /// <summary>
        /// Handed back where the bind was refused, so the chain the caller wrote still reads. It is
        /// not in the binder and nothing can reach it, so the steps hung off it go nowhere - which
        /// is the point: the first chain is the one that runs.
        /// </summary>
        private ICommandBinding DiscardedBinding(ISignalBody key)
        {
            CommandBinding discarded = new CommandBinding();
            discarded.SetKey(key);
            discarded.SetContext(Context);
            return discarded;
        }

        private void ReleaseOwnership(ISignalBody signal)
        {
            if (ReferenceEquals(signal.InternalCallback?.Target, this))
                signal.InternalCallback = null;
        }

        private void LogSignalAlreadyOwned(ISignalBody key, Action<ISignalBody, object[]> boundElsewhere)
        {
            string owner = boundElsewhere.Target is CommandBinder ownerBinder && ownerBinder.Context != null
                ? ownerBinder.Context.GetType().Name
                : "another context";
            string here = Context == null ? "this context" : Context.GetType().Name;

            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► Signal is already bound to commands!</color></b>\n" +
                "<b><color=#FF6666>► Signal:</color><color=#FFEFD5> " + key.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Owned by:</color><color=#FFEFD5> " + owner + "</color></b>\n" +
                "<b><color=#FF6666>► Also bound in:</color><color=#FFEFD5> " + here + "</color></b>\n" +
                "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the commands bound here will not run. " +
                "Move them into " + owner + "'s sequence, or dispatch a signal of this module's own.</color></b>",
                "Signal '" + key.Name + "' is already bound to commands in " + owner +
                ", so the commands bound in " + here + " will not run.");
        }

        private void InitializeGroupWithSignal(ISignalBody signal, params object[] commandParameters)
        {
            ICommandBinding binding = GetBinding(signal);
            if (binding == null)
            {
                FlowLogger.LogWarning(SystemLogType.CommandOperation, $"<b>[CommandBinder]</b> No binding found for signal: {signal.GetType().Name}");
                return;
            }

            ICommandGroupResolver commandGroupResolver = GetAvailableGroup();
            commandGroupResolver.GroupExecutionFinished += _returnGroupToPool;

            // A dispatch is one flow, and it starts here rather than inside the resolver so that
            // the line announcing the group belongs to the same flow as the commands under it.
            // A signal dispatched from inside a command takes the running flow as its parent, and
            // the console builds the tree out of those two ids.
            int flowId = 0, parentFlowId = 0;
            FlowLogger.NextFlowId(ref flowId, ref parentFlowId);

            int previousFlowId = 0, previousParentFlowId = 0;
            FlowLogger.EnterFlow(flowId, parentFlowId, ref previousFlowId, ref previousParentFlowId);
            try
            {
                if (!signal.HideCommandLog)
                    FlowLogger.Log(SystemLogType.CommandOperation, "[CommandGroup][InitializeGroupWithSignal] : '", signal.Name, "'.");
                commandGroupResolver.Initialize(binding, this, commandParameters);
            }
            finally
            {
                FlowLogger.ExitFlow(previousFlowId, previousParentFlowId);
            }
        }

        #region CommandGroupPool

        internal ICommandGroupResolver GetAvailableGroup()
        {
            if (_commandGroupPool.TryPop(out ICommandGroupResolver group))
            {
                return group;
            }

            return new CommandGroup.CommandGroupResolver();
        }

        internal void ReturnGroupToPool(ICommandGroupResolver groupResolver)
        {
            bool hideLog = groupResolver is CommandGroup.CommandGroupResolver concrete && concrete.IsHideLog;

            groupResolver.Dispose();
            _commandGroupPool.Push(groupResolver);

            if (!hideLog)
                FlowLogger.Log(SystemLogType.CommandOperation, "CommandGroup is returned to pool!");
        }

        #endregion

        #region CommandPool

        internal CommandBody GetCommand(Type commandType)
        {
            return _commandPool.TryTake(commandType, out CommandBody command)
                ? command
                : (CommandBody) Activator.CreateInstance(commandType);
        }

        internal void ReturnCommandToPool(ICommandBody commandBody)
        {
            commandBody.Clean();
            Type commandType = commandBody.GetType();

            // Safe by construction: a step type is constrained to CommandBody, and the pool only
            // ever sees back what GetCommand handed out.
            _commandPool.Return(commandType, (CommandBody) commandBody);
            if (!HasHideCommandLog(commandType))
                FlowLogger.Log(SystemLogType.CommandOperation, "Command is returned to pool! - ", commandType.Name);
        }

        public bool HasHideCommandLog(Type type)
        {
            if (!_hideCommandLogCache.TryGetValue(type, out bool isHideCommandLog))
            {
                isHideCommandLog = Attribute.IsDefined(type, typeof(HideCommandLogAttribute));
                _hideCommandLogCache[type] = isHideCommandLog;
            }

            return isHideCommandLog;
        }

        #endregion

        public new CommandBinding GetBinding(object key)
        {
            return base.GetBinding(key);
        }
    }
}