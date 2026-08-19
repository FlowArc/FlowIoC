using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Bind.Bindings.Pool
{
    public class BindingPoolController
    {
        private readonly Dictionary<Type, Queue<IBinding>> _pool = new();

        private Queue<IBinding> GetPoolQueue(Type bindingType)
        {
            if (_pool.TryGetValue(bindingType, out Queue<IBinding> queue)) return queue;
            queue = new Queue<IBinding>();
            _pool[bindingType] = queue;

            return queue;
        }

        internal IBinding GetAvailableBinding(Type bindingType)
        {
            Queue<IBinding> bindingQueue = GetPoolQueue(bindingType);

            if (bindingQueue.Count == 0)
                return (IBinding)Activator.CreateInstance(bindingType);

            return bindingQueue.Dequeue();
        }

        internal TBindingType GetAvailableBinding<TBindingType>()
            where TBindingType : IBinding
        {
            Type bindingType = typeof(TBindingType);
            Queue<IBinding> bindingQueue = GetPoolQueue(bindingType);

            if (bindingQueue.Count == 0)
                return (TBindingType)Activator.CreateInstance(bindingType);

            return (TBindingType)bindingQueue.Dequeue();
        }

        internal void ReturnBindingToPool(IBinding binding)
        {
            Type bindingType = binding.GetType();
            Queue<IBinding> poolQueue = GetPoolQueue(bindingType);
            binding.Clear();
            poolQueue.Enqueue(binding);
        }
    }
}