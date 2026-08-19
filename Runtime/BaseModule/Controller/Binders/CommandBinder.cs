using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Bind.Binders;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.CommandGroup;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Controller.Binders
{
    [HideInModelViewer]
    public class CommandBinder : Binder<CommandBinding>, ICommandBinder
    {
        private readonly Dictionary<Type, bool> _hideCommandLogCache = new();
        private readonly Dictionary<Type, Stack<ICommandBody>> _commandPool = new();
        private readonly Stack<ICommandGroupResolver> _commandGroupPool = new();
        private readonly HashSet<ICommandGroupResolver> _activeCommandGroup = new();

        internal IContext Context;

        public virtual ICommandBinding Bind<TSignal>(TSignal key)
            where TSignal : ISignalBody
        {
            key.InternalCallback = InitializeGroupWithSignal;
            CommandBinding binding = base.Bind(key);
            binding.SetContext(Context);
            return binding;
        }

        private void InitializeGroupWithSignal(ISignalBody signal, params object[] commandParameters)
        {
            ICommandBinding binding = GetBinding(signal);
            if (binding == null)
            {
                FlowLogger.LogWarning(SystemLogType.CommandOperation,$"<b>[CommandBinder]</b> No binding found for signal: {signal.GetType().Name}");
                return;
            }

            ICommandGroupResolver commandGroupResolver = GetAvailableGroup();
            commandGroupResolver.GroupExecutionFinished += ReturnGroupToPool;
            _activeCommandGroup.Add(commandGroupResolver);

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
            _activeCommandGroup.Remove(groupResolver);
            _commandGroupPool.Push(groupResolver);

            if (!hideLog)
                FlowLogger.Log(SystemLogType.CommandOperation, $"CommandGroup is returned to pool!");
        }

        #endregion

        #region CommandPool

        internal ICommandBody GetCommand(Type commandType)
        {
            if (_commandPool.TryGetValue(commandType, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            return (ICommandBody) Activator.CreateInstance(commandType);
        }

        internal void ReturnCommandToPool(ICommandBody commandBody)
        {
            commandBody.Clean();
            Type commandType = commandBody.GetType();

            if (!_commandPool.TryGetValue(commandType, out Stack<ICommandBody> stack))
            {
                stack = new Stack<ICommandBody>();
                _commandPool.Add(commandType, stack);
            }

            stack.Push(commandBody);
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