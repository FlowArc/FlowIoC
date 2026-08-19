using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Bind.Bindings.Pool;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Root
{
    public class RootsManager : IRootsManager
    {
        public InjectionBinderCrossContext InjectionBinderCrossContext { get; private set; }
        public MediatorCreatorController MediatorCreatorController { get; private set; }
        public BindingPoolController BindingPoolController { get; private set; }

        public Action<IContext> OnContextReady;

        private readonly List<IRoot> _contextRootList = new List<IRoot>();
        private readonly Dictionary<string,IRoot> _contextRootMap = new ();
        
        public IRoot GetRootByName(string name) => _contextRootMap[name];

        public void Initialize()
        {
            FlowLogger.Log(SystemLogType.Context, "RootsManager | Initialize Started!");

            BindingPoolController = new BindingPoolController();
            InjectionBinderCrossContext = new InjectionBinderCrossContext();
            MediatorCreatorController = new MediatorCreatorController();

            FlowLogger.Log(SystemLogType.Context, "RootsManager | Initialize Completed!");
        }

        public void Register(IRoot root)
        {
            if (!_contextRootList.Contains(root))
            {
                _contextRootList.Add(root);
                _contextRootMap[root.Name] = root;
                FlowLogger.Log(SystemLogType.Context, "RootsManager | " + root.GetType().Name + " is Registered!");
            }
        }

        public void UnRegister(IRoot root)
        {
            if (_contextRootMap.Remove(root.Name)) 
                FlowLogger.Log(SystemLogType.Context, "RootsManager | " + root.GetType().Name + " is Unregistered!");
            if (_contextRootList.Remove(root)) 
                FlowLogger.Log(SystemLogType.Context, "RootsManager | " + root.GetType().Name + " is Unregistered!");
        }

        public void StartContexts()
        {
            var unreadyContextList = _contextRootList.Where(context => !context.GetContext().IsStarted)
                .OrderBy(x => x.GetContext().InitializeOrder)
                .ToArray();

            foreach (var contextRoot in unreadyContextList)
            {
                contextRoot.StartContext();
            }

            ICoroutineProvider coroutineProvider = (ICoroutineProvider) InjectionBinderCrossContext.GetInstance(typeof(ICoroutineProvider));
            coroutineProvider.WaitForEndOfFrame(() =>
            {
                foreach (IRoot contextRoot in unreadyContextList)
                {
                    contextRoot.Setup();
                }

                foreach (IRoot contextRoot in unreadyContextList)
                {
                    contextRoot.Launch();
                }
            });
        }

        // public bool IsContextReady(IContext context)
        // {
        //     IRoot foundContext = _contextRootList.FirstOrDefault(contextRoot => contextRoot.GetContext() == context);
        //     return foundContext != null && foundContext.GetContext().ContextStarted;
        // }
    }
}