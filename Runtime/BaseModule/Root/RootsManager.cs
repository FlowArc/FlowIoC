using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Bind.Bindings.Pool;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Root.Utils;
using FlowIoC.BaseModule.SharedData;
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

        /// <summary>
        /// The run's shared assets, filed by each Root as it registers and bound for every context
        /// as ISharedDataModel. Owned here because the manager is the one object a run has before
        /// any Root's context exists.
        /// </summary>
        public SharedDataModel SharedDataModel { get; private set; }

        /// <summary>
        /// Bumped whenever any binder in the run gains or loses a binding. What a pooled command
        /// resolved is remembered against this number, so injection is repeated when the container
        /// has actually changed and skipped when it has not.
        /// </summary>
        internal int BindingGeneration;

        /// <summary>
        /// The run's lookup from a context's full name to its type. Owned here so it is built once
        /// and goes away with the run, rather than every Root asking the domain for its types.
        /// </summary>
        internal ContextTypeIndex ContextTypes { get; private set; }

        private readonly List<IRoot> _contextRootList = new List<IRoot>();
        private readonly Dictionary<string, IRoot> _contextRootMap = new();

        /// <summary>
        /// The Root a name belongs to, or null when the scene holds no such Root. Null rather
        /// than a thrown key, because the name is authored in an inspector: a typo is a thing to
        /// report and carry on from, not a thing to stop the object over.
        /// </summary>
        public IRoot GetRootByName(string name) =>
            _contextRootMap.TryGetValue(name, out IRoot root) ? root : null;

        public void Initialize()
        {
            FlowLogger.Log(SystemLogType.Context, "RootsManager | initialize started");

            BindingPoolController = new BindingPoolController();
            InjectionBinderCrossContext = new InjectionBinderCrossContext();
            MediatorCreatorController = new MediatorCreatorController();
            ContextTypes = new ContextTypeIndex();

            SharedDataModel = new SharedDataModel();
            InjectionBinderCrossContext.BindInstance<ISharedDataModel>(SharedDataModel);

            FlowLogger.Log(SystemLogType.Context, "RootsManager | initialize completed");
        }

        public void Register(IRoot root)
        {
            if (_contextRootList.Contains(root))
                return;

            if (_contextRootMap.TryGetValue(root.Name, out IRoot sameName) && sameName != root)
            {
                FlowLogger.LogWarning(
                    SystemLogType.Context,
                    "RootsManager | Two Roots share the GameObject name '" + root.Name +
                    "'. GetRootByName will only ever return the one registered last."
                );
            }

            _contextRootList.Add(root);
            _contextRootMap[root.Name] = root;
            FlowLogger.Log(SystemLogType.Context, "RootsManager | " + root.GetType().Name + " registered");

            // Awake, before any binding phase: what a Root shares is readable from the first
            // PostConstruct on, whichever Root that is.
            SharedDataModel.Register(root, root.SharedScriptables, root.SharedMonoBehaviours);
        }

        public void UnRegister(IRoot root)
        {
            SharedDataModel.UnRegister(root);

            // The map is keyed by name, so removing by key alone would evict whichever Root answers
            // to that name - not necessarily the one being unregistered.
            if (_contextRootMap.TryGetValue(root.Name, out IRoot mapped) && mapped == root)
                _contextRootMap.Remove(root.Name);

            if (_contextRootList.Remove(root))
                FlowLogger.Log(SystemLogType.Context, "RootsManager | " + root.GetType().Name + " unregistered");
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
    }
}