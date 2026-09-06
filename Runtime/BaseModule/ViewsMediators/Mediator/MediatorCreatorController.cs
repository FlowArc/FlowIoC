using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.ViewsMediators.Mediator
{
    /// <summary>
    /// Hands out the Mediator for a View and takes it back when the View goes. A Mediator is
    /// reused, so whatever <c>OnRegister</c> did has to be undone in <c>OnRemove</c>: the View and
    /// the signal holders are injected again on the way out, but a subscription the Mediator made
    /// itself, or anything it wrote to a plain field, is still there when it comes back.
    /// </summary>
    public class MediatorCreatorController
    {
        private readonly Dictionary<Type, Stack<IMediator>> _pool = new();

        // Membership beside the stack. The guard against pooling the same Mediator twice used to
        // walk the stack, and a screen with a long list of pooled rows walked it once per row.
        private readonly HashSet<IMediator> _pooled = new();

        public IMediator GetMediator(Type mediatorType)
        {
            if (_pool.TryGetValue(mediatorType, out Stack<IMediator> mediatorStack) && mediatorStack.Count > 0)
            {
                IMediator mediator = mediatorStack.Pop();
                _pooled.Remove(mediator);
                return mediator;
            }

            return (IMediator) Activator.CreateInstance(mediatorType);
        }

        public void ReturnMediatorToPool(IMediator mediator)
        {
            if (mediator == null || !_pooled.Add(mediator))
                return;

            Type mediatorType = mediator.GetType();

            if (!_pool.TryGetValue(mediatorType, out Stack<IMediator> mediatorStack))
            {
                mediatorStack = new Stack<IMediator>();
                _pool[mediatorType] = mediatorStack;
            }

            mediatorStack.Push(mediator);
        }

        /// <summary>
        /// Lets go of everything parked. What a run built is of no use to the next one, and a burst
        /// of pooled Mediators would otherwise be held for as long as the run lasts.
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
            _pooled.Clear();
        }
    }
}