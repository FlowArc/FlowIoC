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

        protected IContext _boundContext;

        protected List<IConstructable> _constructables;

        protected RootsManager _rootsManager;

        // What an assignable-type scan settled on last time, misses kept as null. Cleared whenever
        // the container changes, which is the only thing that can make an answer wrong.
        private readonly Dictionary<Type, Type> _assignableTypes = new();

        public InjectionBinder()
        {
            _container = new Dictionary<Type, List<InjectionBinding>>();
            _constructables = new List<IConstructable>();
            _rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;
            _bindingPoolController = _rootsManager?.BindingPoolController;
        }

        #region Bind

        public void SetBoundContext(IContext context)
        {
            _boundContext = context;
        }

        /// <summary>
        /// Whether something is bound under this type and name. The quiet question: GetInstance
        /// reports a miss, because whoever asks it expects an answer, and this is for the caller
        /// who only wants to know before binding one itself.
        /// </summary>
        public bool HasBinding<TBindingType>(string name = "") => HasInstanceExist<TBindingType>(name);

        /// <summary>
        /// How many times any binder in this run has gained or lost a binding. Injection results
        /// are remembered against it, so what a pooled command resolved stays good until the
        /// container it resolved from actually changes.
        /// </summary>
        internal int BindingGeneration => _rootsManager?.BindingGeneration ?? 0;

        public TBindingType Bind<TBindingType>(string name = "")
            where TBindingType : new()
        {
            FlowLogger.Log(SystemLogType.Injection,
                _boundContext.GetType().Name + " | Binding: " + typeof(TBindingType).Name + (name != "" ? (" Name: " + name) : ""));
            return GetOrCreateInstance<TBindingType>(name);
        }

        public TAbstract Bind<TAbstract, TConcrete>(string name = "")
            where TConcrete : TAbstract, new()
        {
            FlowLogger.Log(SystemLogType.Injection,
                _boundContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name + (name != "" ? (" Name: " + name) : ""));
            return GetOrCreateInstance<TAbstract, TConcrete>(name);
        }

        public TAbstract Bind<TAbstract, TConcrete, TDummy>(string name = "")
            where TConcrete : TAbstract, new()
            where TDummy : TAbstract, new()
        {
            // The dummy is an Editor affordance only: a build always gets the real implementation,
            // whatever a Root left ticked in a scene.
#if UNITY_EDITOR
            if (_boundContext.IsTest)
            {
                FlowLogger.Log(SystemLogType.Injection,
                    _boundContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name +
                    (name != "" ? " Name: " + name : "") + " To: " + typeof(TDummy).Name);
                return GetOrCreateInstance<TAbstract, TDummy>(name);
            }
#endif
            FlowLogger.Log(SystemLogType.Injection,
                _boundContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name +
                (name != "" ? " Name: " + name : "") + " To: " + typeof(TConcrete).Name);
            return GetOrCreateInstance<TAbstract, TConcrete>(name);
        }

        public void BindInstance(object instance, string name = "")
        {
            object hasInstanceExist = GetInstance(instance.GetType(), name);
            if (hasInstanceExist != null)
            {
                FlowLogger.LogWarning(SystemLogType.Injection,
                    _boundContext.GetType().Name + " | There is a same injection! Type: " + instance.GetType().Name +
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
                _boundContext.GetType().Name + " | Binding: " + injectionType.Name + (name != "" ? (" Name: " + name) : ""));
            _container[injectionType].Add(injectionBinding);
            NoteContainerChanged();
        }

        public void BindInstance<TAbstract>(object instance, string name = "")
        {
            Type injectionType = typeof(TAbstract);
            object hasInstanceExist = GetInstance(injectionType, name);
            if (hasInstanceExist != null)
            {
                FlowLogger.LogWarning(SystemLogType.Injection,
                    _boundContext.GetType().Name + " | There is a same injection! Type: " + typeof(TAbstract).Name +
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
                _boundContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name + (name != "" ? (" Name: " + name) : ""));
            _container[injectionType].Add(injectionBinding);
            NoteContainerChanged();
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
                    _boundContext.GetType().Name + " | There is a same injection! Type: " + typeof(TAbstract).Name +
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

        /// <summary>
        /// Empties the container. The inner loop used to advance its index while UnBind removed
        /// the entry it had just read, so a type bound under more than one name kept about half of
        /// them: their Deconstruct never ran and their bindings never reached the pool.
        /// </summary>
        public virtual void UnBindAll()
        {
            List<Type> keys = _container.Keys.ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                Type keyType = keys[i];

                if (!_container.TryGetValue(keyType, out List<InjectionBinding> bindings))
                    continue;

                InjectionBinding[] snapshot = bindings.ToArray();

                for (int ii = 0; ii < snapshot.Length; ii++)
                {
                    InjectionBinding injectionBinding = snapshot[ii];
                    Type type = injectionBinding.Key as Type;
                    Type key = type ?? injectionBinding.Key.GetType();

                    UnBind(key, injectionBinding.Name);
                }
            }
        }

        /// <summary>
        /// Takes back everything a context bound here. The context calls this on its way out: the
        /// shared binder outlives every context, so without it a module's holder and Service
        /// outlived the module - and when its Root was built again, the old instances came back,
        /// pointing at models the old context had already torn down. What was handed in with
        /// BindInstance has no context and is not touched.
        /// </summary>
        public virtual void UnBindAllBoundBy(IContext context)
        {
            if (context == null)
                return;

            List<InjectionBinding> owned = new List<InjectionBinding>();

            foreach (KeyValuePair<Type, List<InjectionBinding>> bound in _container)
            {
                for (int i = 0; i < bound.Value.Count; i++)
                {
                    if (bound.Value[i].BoundContext == context)
                        owned.Add(bound.Value[i]);
                }
            }

            for (int i = 0; i < owned.Count; i++)
            {
                Type key = owned[i].Key as Type ?? owned[i].Key.GetType();
                UnBind(key, owned[i].Name);
            }
        }

        public virtual void UnBind<TBindingType>(string name = "")
        {
            bool hasBindingExist = HasInstanceExist<TBindingType>(name);
            if (!hasBindingExist)
                return;

            UnBind(typeof(TBindingType), name);
        }

        /// <summary>
        /// Takes out the binding that holds this instance. An instance nobody bound is left alone:
        /// this used to test the wrong variable for null and then read the binding it had not
        /// found, so unbinding something that was never bound threw instead of doing nothing.
        /// </summary>
        public virtual void UnBind<TBindingType>(object injectedObject)
        {
            InjectionBinding injectionBinding = GetInjectionBinding<TBindingType>(injectedObject);
            if (injectionBinding == null)
                return;

            UnBind(typeof(TBindingType), injectionBinding.Name);
        }

        protected void UnBind(Type key, string name = "")
        {
            InjectionBinding injectionBinding = GetInjectionBinding(key, name);
            if (injectionBinding == null)
                return;

            List<InjectionBinding> bindings = _container[key];
            bindings.Remove(injectionBinding);

            // A type with nothing left under it is not bound, and is answered for as such rather
            // than as "bound, but not under this name".
            if (bindings.Count == 0)
                _container.Remove(key);

            NoteContainerChanged();

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
                FlowLogger.LogError(SystemLogType.Injection, "Nothing is bound to " + bindingType.Name
                                                                                    + NameSuffix(name) + ". Whoever owns it either never "
                                                                                    + "bound it or is not in the scene.");
                return default;
            }

            InjectionBinding injectionData = bindings.FirstOrDefault(x => x.Name == name);

            if (injectionData == null)
            {
                FlowLogger.LogError(SystemLogType.Injection,
                    bindingType.Name + " is bound, but not" + NameSuffix(name) + ".");
                return default;
            }

            return (TBindingType) injectionData.Value;
        }

        private string NameSuffix(string name) => name == "" ? "" : " under the name '" + name + "'";

        /// <summary>
        /// The instance bound to a type, or to something assignable to it, or null when this binder
        /// holds neither. Injection asks every context in turn, so most calls land on a binder that
        /// has nothing - and the miss used to cost a copy of the whole key set before the scan even
        /// started. What the scan settles on is remembered instead, misses included, and forgotten
        /// again whenever a binding is added or taken away.
        /// </summary>
        public object GetInstance(Type instanceType, string name = "")
        {
            if (!_container.TryGetValue(instanceType, out List<InjectionBinding> values))
            {
                Type assignedType = ResolveAssignableType(instanceType);
                if (assignedType == null)
                    return null;

                values = _container[assignedType];
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Name == name)
                    return values[i].Value;
            }

            return null;
        }

        private Type ResolveAssignableType(Type instanceType)
        {
            if (_assignableTypes.TryGetValue(instanceType, out Type remembered))
                return remembered;

            Type assignedType = null;
            foreach (KeyValuePair<Type, List<InjectionBinding>> bound in _container)
            {
                if (!instanceType.IsAssignableFrom(bound.Key))
                    continue;

                assignedType = bound.Key;
                break;
            }

            _assignableTypes[instanceType] = assignedType;
            return assignedType;
        }

        /// <summary>
        /// Says the container's shape changed. Injection results are remembered against this, so a
        /// binding added or removed is what makes anything holding an older number resolve again.
        /// </summary>
        private void NoteContainerChanged()
        {
            _assignableTypes.Clear();
            if (_rootsManager != null)
                _rootsManager.BindingGeneration++;
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
            injectionBinding.BoundContext = _boundContext;

            _container[injectionType].Add(injectionBinding);
            NoteContainerChanged();
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
            injectionBinding.BoundContext = _boundContext;

            _container[injectionType].Add(injectionBinding);
            NoteContainerChanged();
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

        /// <summary>
        /// Runs PostConstruct on everything bound that has one. Indexed rather than a foreach,
        /// because this is the phase where a module puts its data in place and a PostConstruct is
        /// allowed to bind - which grows the list it is being read from.
        /// </summary>
        public void RunPostConstructs()
        {
            for (int i = 0; i < _constructables.Count; i++)
            {
                IConstructable constructable = _constructables[i];

                if (constructable.IsPostConstructed) continue;
                if (constructable.IsDeConstructed) continue;

                constructable.PostConstruct();
                constructable.IsPostConstructed = true;
            }
        }

        public List<InjectionBinding> GetAllInjectionBindings()
        {
            List<InjectionBinding> all = new List<InjectionBinding>();

            foreach (KeyValuePair<Type, List<InjectionBinding>> bound in _container)
                all.AddRange(bound.Value);

            return all;
        }

        internal InjectionBinding GetInjectionBinding(Type key, string name = "")
        {
            return _container.TryGetValue(key, out List<InjectionBinding> bindings)
                ? bindings.FirstOrDefault(x => x.Name == name)
                : null;
        }

        internal InjectionBinding GetInjectionBinding<TBindingType>(object value)
        {
            return _container.TryGetValue(typeof(TBindingType), out List<InjectionBinding> bindings)
                ? bindings.FirstOrDefault(x => x.Value == value)
                : null;
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