using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Bind.Bindings.Pool;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Bind.Binders
{
    public class Binder<TBindingType> : IBinder<TBindingType>
        where TBindingType : IBinding, new()
    {
        private readonly BindingPoolController _bindingPoolController;
        private readonly Dictionary<object, TBindingType> _bindings;

        protected Binder()
        {
            _bindings = new Dictionary<object, TBindingType>();
            RootsManager rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;
            _bindingPoolController = rootsManager?.BindingPoolController;
        }

        #region Bind

        public virtual TBindingType Bind<TKeyType>()
        {
            Type keyType = typeof(TKeyType);
            if (_bindings.ContainsKey(keyType))
            {
                FlowLogger.LogWarning(SystemLogType.Injection, "Binding already exists! keyType: " + keyType.Name);
                return default;
            }

            TBindingType binding = (TBindingType)_bindingPoolController.GetAvailableBinding(typeof(TBindingType));
            binding.SetKey(keyType);
            _bindings.Add(keyType, binding);
            return binding;
        }

        public virtual TBindingType Bind(object key)
        {
            if (_bindings.ContainsKey(key))
            {
                FlowLogger.LogWarning(SystemLogType.Injection, "Binding already exists! keyType: " + key.GetType());
                return default;
            }

            TBindingType binding = (TBindingType)_bindingPoolController.GetAvailableBinding(typeof(TBindingType));
            binding.SetKey(key);
            _bindings.Add(key, binding);
            return binding;
        }

        #endregion

        #region UnBind

        public virtual void UnBind(object key)
        {
            if (_bindings.TryGetValue(key, out TBindingType binding))
            {
                UnBind(binding);
            }
        }

        public virtual void UnBindAll()
        {
            foreach (var binding in _bindings.Values.ToList())
            {
                UnBind(binding);
            }
        }

        private void UnBind(IBinding binding)
        {
            _bindings.Remove(binding.Key);
            _bindingPoolController.ReturnBindingToPool(binding);
            FlowLogger.LogWarning(SystemLogType.Injection, "UnBind: " + binding.Key);
        }

        #endregion

        #region GetBinding

        public TBindingType GetBinding(object key)
        {
            _bindings.TryGetValue(key, out TBindingType binding);
            return binding;
        }

        public TBindingType GetBinding<TKeyType>()
        {
            Type keyType = typeof(TKeyType);
            _bindings.TryGetValue(keyType, out TBindingType binding);
            return binding;
        }

        #endregion
    }
}