using System;
using FlowIoC.BaseModule.Pooling;

namespace FlowIoC.BaseModule.Bind.Bindings.Pool
{
    public class BindingPoolController
    {
        private readonly TypePool<IBinding> _pool = new();

        internal IBinding GetAvailableBinding(Type bindingType)
        {
            return _pool.TryTake(bindingType, out IBinding binding)
                ? binding
                : (IBinding) Activator.CreateInstance(bindingType);
        }

        internal TBindingType GetAvailableBinding<TBindingType>()
            where TBindingType : IBinding
        {
            return (TBindingType) GetAvailableBinding(typeof(TBindingType));
        }

        internal void ReturnBindingToPool(IBinding binding)
        {
            if (binding == null)
                return;

            // Cleared before it is parked, not after it is taken: a binding holding its last key
            // and value is a binding still pointing at whatever was unbound.
            binding.Clear();
            _pool.Return(binding.GetType(), binding);
        }

        internal void Clear() => _pool.Clear();
    }
}