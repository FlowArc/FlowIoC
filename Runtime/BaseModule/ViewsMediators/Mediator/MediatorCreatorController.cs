using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.ViewsMediators.Mediator
{
    public class MediatorCreatorController
    {
        private readonly Dictionary<Type, Stack<IMediator>> _pool;

        public MediatorCreatorController()
        {
            _pool = new Dictionary<Type, Stack<IMediator>>();
        }

        public IMediator GetMediator(Type mediatorType)
        {
            if (!_pool.TryGetValue(mediatorType, out Stack<IMediator> mediatorStack))
            {
                mediatorStack = new Stack<IMediator>();
                _pool[mediatorType] = mediatorStack;
            }

            if (mediatorStack.Count > 0)
            {
                return mediatorStack.Pop();
            }

            return (IMediator)Activator.CreateInstance(mediatorType);
        }

        public void ReturnMediatorToPool(IMediator mediator)
        {
            Type mediatorType = mediator.GetType();

            if (!_pool.TryGetValue(mediatorType, out Stack<IMediator> mediatorStack))
            {
                mediatorStack = new Stack<IMediator>();
                _pool[mediatorType] = mediatorStack;
            }

            if (!mediatorStack.Contains(mediator))
            {
                mediatorStack.Push(mediator);
            }
        }
    }
}