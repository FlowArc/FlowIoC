using System.Collections.Generic;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using UnityEngine;

namespace FlowIoC.BaseModule.Contexts
{
    public class Context : IContext
    {
        private GameObject _gameObject;
        private SignalParamResolver _signalParamResolver;
        public bool IsStarted { get; set; }
        public MediationBinder MediationBinder { get; set; }
        public InjectionBinder InjectionBinder { get; set; }
        public InjectionBinderCrossContext InjectionBinderCrossContext { get; set; }
        public ICommandBinder CommandBinder { get; set; }
        public List<IContext> SubContexts { get; set; }
        public List<IContext> AllContexts { get; set; }
        public bool IsTest { get; set; }

        public int InitializeOrder { get; set; }

        public void Initialize(GameObject contextGameObject, int initializeOrder, InjectionBinderCrossContext injectionBinderCrossContext,
            List<IContext> subContexts, bool isTest = false)
        {
            _gameObject = contextGameObject;
            InitializeOrder = initializeOrder;
            InjectionBinderCrossContext = injectionBinderCrossContext;
            SubContexts = subContexts;
            IsTest = isTest;

            // A list of its own. AllContexts used to be the same object as SubContexts with this
            // context pushed onto the front, so a context appeared among its own sub-contexts and
            // anything walking SubContexts met the parent as well.
            AllContexts = new List<IContext>(subContexts.Count + 1) {this};
            AllContexts.AddRange(subContexts);
        }

        public void Start()
        {
            IsStarted = true;

            CoreBindings();
        }

        void IContext.InjectAllInstances()
        {
            Inject(InjectionBinder.GetAllInjectionBindings());
            Inject(InjectionBinderCrossContext.GetAllInjectionBindings());
        }

        private void Inject(List<InjectionBinding> bindings)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                InjectionBinding binding = bindings[i];

                if (binding == null)
                    continue;

                if (binding.BindedContext == null)
                    this.TryToInjectObject(binding.Value);
                else
                    binding.TryToInjectObject();
            }
        }

        SignalParamResolver IContext.SignalParamResolver
            => _signalParamResolver ??= new SignalParamResolver();

        void IContext.ExecutePostConstructMethods()
        {
            /*
            List<InjectionBinding> injectedTypes = InjectionBinder.GetAllInjectionBindings();
            List<InjectionBinding> injectedCrossContextTypes = InjectionBinderCrossContext.GetAllInjectionBindings();

            injectedTypes = injectedTypes.Concat(injectedCrossContextTypes).ToList();

            foreach (InjectionBinding injectedType in injectedTypes)
            {
                if (InjectionBinderCrossContext.PostConstructedObjects.Contains(injectedType.Value))
                    continue;

                PostConstructUtils.ExecutePostConstructMethod(injectedType.Value);

                InjectionBinderCrossContext.PostConstructedObjects.Push(injectedType.Value);
            }
            */
            InjectionBinder.RunPostConstructs();
            InjectionBinderCrossContext.RunPostConstructs();
        }

        protected virtual void CoreBindings()
        {
            InjectionBinder = new InjectionBinder();
            InjectionBinder.SetBindedContext(this);

            InjectionBinderCrossContext.SetBindedContext(this);
            InjectionBinderCrossContext.BindInstance(InjectionBinderCrossContext);

            MediationBinder = InjectionBinder.Bind<MediationBinder>();

            CommandBinder = InjectionBinder.Bind<ICommandBinder, CommandBinder>();
            ((CommandBinder) CommandBinder).Context = this;

            InjectionBinderCrossContext.BindInstance<GameObject>(_gameObject, GetType().Name);
            InjectionBinder.BindInstance<GameObject>(_gameObject, nameof(IContext));

            InjectionBinderCrossContext.BindMonoBehaviorInstance<IUpdateProvider, UpdateProvider>();
            InjectionBinderCrossContext.BindMonoBehaviorInstance<ICoroutineProvider, CoroutineProvider>();

            FunctionProvider functionProvider = (FunctionProvider) InjectionBinder.Bind<IFunctionProvider, FunctionProvider>();
            functionProvider.Context = this;
        }

        public virtual void SignalBindings()
        {
        }

        public virtual void InjectionBindings()
        {
        }

        public virtual void MediationBindings()
        {
        }

        public virtual void CommandBindings()
        {
        }

        public virtual void Setup()
        {
        }

        public virtual void Launch()
        {
        }

        public virtual void DestroyContext()
        {
            IsStarted = false;

            InjectionBinderCrossContext.UnBind<GameObject>(GetType().Name);

            MediationBinder?.UnBindAll();
            CommandBinder?.UnBindAll();
            InjectionBinder?.UnBindAll();
        }

        public virtual void PauseContext()
        {
        }

        public virtual void ResumeContext()
        {
        }
    }
}