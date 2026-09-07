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

        internal IContext Context;

        /// <summary>
        /// A signal carries one command callback, so the first Context to bind it owns it. A second
        /// Context binding the same signal used to overwrite that callback without a word, and the
        /// first Context's commands then never ran. The binding is still handed back so the chain
        /// that follows reads normally, but the signal keeps the owner it already had.
        /// </summary>
        public virtual ICommandBinding Bind<TSignal>(TSignal key)
            where TSignal : ISignalBody
        {
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
            commandGroupResolver.GroupExecutionFinished += ReturnGroupToPool;

            if (!signal.HideCommandLog)
                FlowLogger.Log(SystemLogType.CommandOperation, $"[CommandGroup][InitializeGroupWithSignal] : '{signal.Name}'.");
            commandGroupResolver.Initialize(binding, this, commandParameters);
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
                FlowLogger.Log(SystemLogType.CommandOperation, $"CommandGroup is returned to pool!");
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
                FlowLogger.Log(SystemLogType.CommandOperation, $"Command is returned to pool! - {commandType.Name}");
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