using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Bind.Bindings.Pool;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Injectable
{
    [HideInModelViewer]
    public class InjectionBinder
    {
        protected Dictionary<Type, List<InjectionBinding>> _container;

        protected BindingPoolController _bindingPoolController;

        protected IContext _bindedContext;

        protected List<IConstructable> _constructables;

        public InjectionBinder()
        {
            _container = new Dictionary<Type, List<InjectionBinding>>();
            _constructables = new List<IConstructable>();
            RootsManager rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;
            _bindingPoolController = rootsManager?.BindingPoolController;
        }

        #region Bind

        public void SetBindedContext(IContext context)
        {
            _bindedContext = context;
        }

        public TBindingType Bind<TBindingType>(string name = "")
            where TBindingType : new()
        {
            FlowLogger.Log(SystemLogType.Injection,
                _bindedContext.GetType().Name + " | Binding: " + typeof(TBindingType).Name + (name != "" ? (" Name: " + name) : ""));
            return GetOrCreateInstance<TBindingType>(name);
        }

        public TAbstract Bind<TAbstract, TConcrete>(string name = "")
            where TConcrete : TAbstract, new()
        {
            FlowLogger.Log(SystemLogType.Injection,
                _bindedContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name + (name != "" ? (" Name: " + name) : ""));
            return GetOrCreateInstance<TAbstract, TConcrete>(name);
        }

        public TAbstract Bind<TAbstract, TConcrete, TDummy>(string name = "")
            where TConcrete : TAbstract, new()
            where TDummy : TAbstract, new()
        {
            // The dummy is an Editor affordance only: a build always gets the real implementation,
            // whatever a Root left ticked in a scene.
#if UNITY_EDITOR
            if (_bindedContext.IsTest)
            {
                FlowLogger.Log(SystemLogType.Injection,
                    _bindedContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name +
                    (name != "" ? (" Name: " + name) : "" + " To: " + typeof(TDummy).Name));
                return GetOrCreateInstance<TAbstract, TDummy>(name);
            }
#endif
            FlowLogger.Log(SystemLogType.Injection,
                _bindedContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name +
                (name != "" ? (" Name: " + name) : "" + " To: " + typeof(TConcrete).Name));
            return GetOrCreateInstance<TAbstract, TConcrete>(name);
        }

        public void BindInstance(object instance, string name = "")
        {
            object hasInstanceExist = GetInstance(instance.GetType(), name);
            if (hasInstanceExist != null)
            {
                FlowLogger.LogWarning(SystemLogType.Injection,
                    _bindedContext.GetType().Name + " | There is a same injection! Type: " + instance.GetType().Name +
                    (name != "" ? (" Name: " + name) : ""));
                return;
            }

            Type injectionType = instance.GetType();

            if (!_container.ContainsKey(injectionType))
                _container.Add(injectionType, new List<InjectionBinding>());

            InjectionBinding injectionBinding = _bindingPoolController.GetAvailableBinding<InjectionBinding>();
            injectionBinding.Name = name;
            injectionBinding.SetValue(instance);
            injectionBinding.SetKey(injectionType);

            FlowLogger.Log(SystemLogType.Injection,
                _bindedContext.GetType().Name + " | Binding: " + injectionType.Name + (name != "" ? (" Name: " + name) : ""));
            _container[injectionType].Add(injectionBinding);
        }

        public void BindInstance<TAbstract>(object instance, string name = "")
        {
            Type injectionType = typeof(TAbstract);
            object hasInstanceExist = GetInstance(injectionType, name);
            if (hasInstanceExist != null)
            {
                FlowLogger.LogWarning(SystemLogType.Injection,
                    _bindedContext.GetType().Name + " | There is a same injection! Type: " + typeof(TAbstract).Name +
                    (name != "" ? (" Name: " + name) : ""));
                return;
            }

            if (!_container.ContainsKey(injectionType))
                _container.Add(injectionType, new List<InjectionBinding>());

            InjectionBinding injectionBinding = _bindingPoolController.GetAvailableBinding<InjectionBinding>();
            injectionBinding.Name = name;
            injectionBinding.SetValue(instance);
            injectionBinding.SetKey(injectionType);

            FlowLogger.Log(SystemLogType.Injection,
                _bindedContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name + (name != "" ? (" Name: " + name) : ""));
            _container[injectionType].Add(injectionBinding);
        }

        public TAbstract BindMonoBehaviorInstance<TAbstract, TConcrete>(string name = "")
            where TConcrete : MonoBehaviour, TAbstract
        {
            bool hasInstanceExist = HasInstanceExist<TAbstract>(name);
            TAbstract instance;
            if (hasInstanceExist)
            {
                instance = GetInstance<TAbstract>(name);
                FlowLogger.LogWarning(SystemLogType.Injection,
                    _bindedContext.GetType().Name + " | There is a same injection! Type: " + typeof(TAbstract).Name +
                    (name != "" ? (" Name: " + name) : ""));
                return instance;
            }

            GameObject instanceGameObject = new GameObject(typeof(TConcrete).Name);
            instance = instanceGameObject.AddComponent<TConcrete>();

            BindInstance<TAbstract>(instance, name);

            return instance;
        }

        #endregion

        #region UnBind

        public virtual void UnBindAll()
        {
            List<Type> keys = _container.Keys.ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                Type keyType = keys[i];

                for (int ii = 0; ii < _container[keyType].Count; ii++)
                {
                    InjectionBinding injectionBinding = _container[keyType][0];
                    Type type = injectionBinding.Key as Type;
                    Type key = type ?? injectionBinding.Key.GetType();

                    UnBind(key, injectionBinding.Name);
                }
            }
        }

        public virtual void UnBind<TBindingType>(string name = "")
        {
            bool hasBindingExist = HasInstanceExist<TBindingType>(name);
            if (!hasBindingExist)
                return;

            UnBind(typeof(TBindingType), name);
        }

        public virtual void UnBind<TBindingType>(object injectedObject)
        {
            InjectionBinding injectionBinding = GetInjectionBinding<TBindingType>(injectedObject);
            if (injectedObject == null)
                return;

            UnBind(typeof(TBindingType), injectionBinding.Name);
        }

        protected void UnBind(Type key, string name = "")
        {
            InjectionBinding injectionBinding = GetInjectionBinding(key, name);
            _container[key].Remove(injectionBinding);

            //DeconstructUtils.ExecuteDeconstructMethod(injectionBinding.Value);
            RunDeconstruct(injectionBinding.Value);

            _bindingPoolController.ReturnBindingToPool(injectionBinding);

            FlowLogger.Log(SystemLogType.Injection, "Unbinding: " + key.Name + (name != "" ? (" Name: " + name) : ""));
        }

        #endregion

        #region GetInstance

        /// <summary>
        /// The instance bound to <typeparamref name="TBindingType"/>, or the default when nothing
        /// is. Asking for something nobody bound is a wiring mistake rather than a question with
        /// an answer - most often a Connector reaching for a module whose Root is not in the scene -
        /// so it is reported by name instead of throwing a dictionary's key error at the caller.
        /// </summary>
        public TBindingType GetInstance<TBindingType>(string name = "")
        {
            Type bindingType = typeof(TBindingType);

            if (!_container.TryGetValue(bindingType, out List<InjectionBinding> bindings))
            {
                Debug.LogError("Nothing is bound to " + bindingType.Name + NameSuffix(name)
                               + ". Whoever owns it either never bound it or is not in the scene.");
                return default;
            }

            InjectionBinding injectionData = bindings.FirstOrDefault(x => x.Name == name);

            if (injectionData == null)
            {
                Debug.LogError(bindingType.Name + " is bound, but not" + NameSuffix(name) + ".");
                return default;
            }

            return (TBindingType) injectionData.Value;
        }

        private string NameSuffix(string name) => name == "" ? "" : " under the name '" + name + "'";

        public object GetInstance(Type instanceType, string name = "")
        {
            Type type = instanceType;
            if (!_container.ContainsKey(instanceType))
            {
                List<Type> injectedTypes = _container.Keys.ToList();
                Type assignedType = injectedTypes.FirstOrDefault(instanceType.IsAssignableFrom);
                if (assignedType == null)
                    return null;

                type = assignedType;
            }

            List<InjectionBinding> values = _container[type];
            InjectionBinding binding = values.FirstOrDefault(x => x.Name == name);
            return binding == null ? null : binding.Value;
        }

        #endregion

        #region GetOrCreateInstance

        protected TBindingType GetOrCreateInstance<TBindingType>(string name = "")
            where TBindingType : new()
        {
            Type bindingType = typeof(TBindingType);
            bool hasInstanceExist = HasInstanceExist<TBindingType>(name);

            TBindingType instance;

            if (!hasInstanceExist)
                instance = CreateInstance<TBindingType>(name);
            else
            {
                instance = (TBindingType) GetInstance(bindingType, name);
                FlowLogger.LogWarning(SystemLogType.Injection,
                    "There is a same injection! Type: " + typeof(TBindingType) + (name != "" ? (" Name: " + name) : ""));
            }

            return instance;
        }

        protected TAbstract GetOrCreateInstance<TAbstract, TConcrete>(string name = "")
            where TConcrete : TAbstract, new()
        {
            Type bindingType = typeof(TAbstract);
            bool hasInstanceExist = HasInstanceExist<TAbstract>(name);

            TAbstract instance;

            if (!hasInstanceExist)
                instance = CreateInstance<TAbstract, TConcrete>(name);
            else
            {
                instance = (TAbstract) GetInstance(bindingType, name);
                FlowLogger.LogWarning(SystemLogType.Injection,
                    "There is a same injection! Type: " + typeof(TAbstract) + (name != "" ? (" Name: " + name) : ""));
            }

            return instance;
        }

        #endregion

        #region CreateInstance

        private TBindingType CreateInstance<TBindingType>(string name = "")
            where TBindingType : new()
        {
            TBindingType instance = new TBindingType();
            Type injectionType = typeof(TBindingType);

            if (!_container.ContainsKey(injectionType))
                _container.Add(injectionType, new List<InjectionBinding>());

            InjectionBinding injectionBinding = _bindingPoolController.GetAvailableBinding<InjectionBinding>();
            injectionBinding.Name = name;
            injectionBinding.SetValue(instance);
            injectionBinding.SetKey(injectionType);
            injectionBinding.BindedContext = _bindedContext;

            _container[injectionType].Add(injectionBinding);
            AddConstructable(instance);

            return instance;
        }

        private TAbstract CreateInstance<TAbstract, TConcrete>(string name = "")
            where TConcrete : TAbstract, new()
        {
            TConcrete instance = new TConcrete();
            Type injectionType = typeof(TAbstract);

            if (!_container.ContainsKey(injectionType))
                _container.Add(injectionType, new List<InjectionBinding>());

            InjectionBinding injectionBinding = _bindingPoolController.GetAvailableBinding<InjectionBinding>();
            injectionBinding.Name = name;
            injectionBinding.SetValue(instance);
            injectionBinding.SetKey(injectionType);
            injectionBinding.BindedContext = _bindedContext;

            _container[injectionType].Add(injectionBinding);
            AddConstructable(instance);

            return instance;
        }

        public void AddConstructable(object injectionBinding)
        {
            if (injectionBinding is IConstructable constructable)
                _constructables.Add(constructable);
        }

        #endregion

        public void RunDeconstruct(object injectionBinding)
        {
            if (injectionBinding is not IConstructable constructable) return;
            if (!constructable.IsPostConstructed) return;
            if (constructable.IsDeConstructed) return;

            constructable.Deconstruct();
            constructable.IsDeConstructed = true;
            constructable.IsPostConstructed = false;
            _constructables.Remove(constructable);
        }

        public void RunPostConstructs()
        {
            foreach (var constructable in _constructables)
            {
                if (constructable.IsPostConstructed) continue;
                if (constructable.IsDeConstructed) continue;

                constructable.PostConstruct();
                constructable.IsPostConstructed = true;
            }
        }

        public List<InjectionBinding> GetAllInjectionBindings()
        {
            return _container.Values
                .ToList()
                .SelectMany(list => list)
                .ToList()
                .Select(x => x)
                .ToList();
        }

        internal InjectionBinding GetInjectionBinding(Type key, string name = "")
        {
            InjectionBinding injectionBinding = _container[key].FirstOrDefault(x => x.Name == name);
            return injectionBinding;
        }

        internal InjectionBinding GetInjectionBinding<TBindingType>(object value)
        {
            Type key = typeof(TBindingType);
            InjectionBinding injectionBinding = _container[key].FirstOrDefault(x => x.Value == value);
            return injectionBinding;
        }

        protected bool HasInstanceExist<TBindingType>(string name = "")
        {
            Type bindingType = typeof(TBindingType);
            if (!_container.TryGetValue(bindingType, out List<InjectionBinding> instanceList))
                return false;

            return instanceList.FirstOrDefault(x => x.Name == name) != null;
        }
    }
}