using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root.Utils;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Root
{
    public class RootBase : MonoBehaviour, IRoot
    {
        [HideInInspector] public List<SubContextData> SubContextTypes;

        [HideInInspector] protected Dictionary<IContext, SubContextData> _subContexts = new();

        /// <summary>
        /// Where this Root sits in the binding order, from -100 to 100. -100 is the earliest a
        /// Root can be and belongs to whatever must put data in place before anything reads it;
        /// 100 is the latest and belongs to the Root whose Launch starts the game. Services take
        /// the negative band, the game's own modules 0 to 97, the Connector 98, screens 99.
        /// </summary>
        [HideInInspector] public int initializeOrder;

        [HideInInspector] internal bool signalsBound;
        [HideInInspector] internal bool injectionsBound;
        [HideInInspector] internal bool mediationsBound;
        [HideInInspector] internal bool commandsBound;
        [HideInInspector] internal bool hasInitialized;
        [HideInInspector] internal bool hasSetuped;
        [HideInInspector] internal bool hasLaunched;

        /// <summary>
        /// Whether this Root binds its injections when the scene starts. With it off nothing is
        /// injected until something binds them by hand, which is a thing a test does and a
        /// running game does not.
        /// </summary>
        [HideInInspector] public bool AutoBindInjections = true;

        /// <summary>
        /// Whether this Root binds its mediations when the scene starts. With it off, a View in
        /// the scene never gets its Mediator.
        /// </summary>
        [HideInInspector] public bool AutoBindMediations = true;

        /// <summary>
        /// Whether the context is built and its bindings declared without being asked. This is
        /// the phase that runs the module's own PostConstruct, so a module that has to put data
        /// in place before anything reads it does that work here.
        /// </summary>
        [HideInInspector] public bool AutoInitialize = true;

        /// <summary>
        /// Whether Setup runs on its own. Setup runs a frame after every Root in the scene has
        /// finished binding, which is what makes it the only phase allowed to reach across
        /// modules - a Connector does its wiring here.
        /// </summary>
        [HideInInspector] public bool AutoSetup = true;

        /// <summary>
        /// Whether Launch runs on its own. Launch happens after every Setup, and is where a
        /// module dispatches its first signal.
        /// </summary>
        [HideInInspector] public bool AutoLaunch = true;

        /// <summary>
        /// Marks this Root as test scaffolding. A test context may reference any module in the
        /// project, and everything it brings is expected to stay out of a built game.
        /// </summary>
        [HideInInspector] public bool IsTest = false;

        public IContext Context { get; set; }

        protected RootsManager _rootsManager;

        public string Name => transform.name;

        public virtual void StartContext(bool forceToStart = false)
        {
        }

        public virtual void InitializeSubContexts()
        {
            if (SubContextTypes == null || SubContextTypes.Count == 0)
                return;

            _subContexts = new Dictionary<IContext, SubContextData>();

            List<Type> assemblyTypes = AssemblyExtensions.GetAllContextTypes();

            foreach (SubContextData subContextData in SubContextTypes)
            {
                Type contextType = assemblyTypes.FirstOrDefault(x => x.FullName == subContextData.ContextFullName);
                if (contextType == null)
                {
                    FlowLogger.LogError(SystemLogType.Context, "Context Type couldn't find! " + subContextData.ContextFullName);
                    continue;
                }

                IContext context = (IContext) Activator.CreateInstance(contextType);

                // Before any binding phase, so a context that reads its own configuration during
                // one of them already has the Root's word for it.
                if (context is ISubContextOverridable overridable)
                    overridable.ApplyOverride(subContextData);

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.ContextName + " | Initialized");
                context.Initialize(gameObject, initializeOrder, _rootsManager.InjectionBinderCrossContext, new List<IContext>(),
                    subContextData.IsTest);
                _subContexts.Add(context, subContextData);
            }
        }

        protected virtual void BeforeCreateContext()
        {
        }

        protected virtual void AfterCreateBeforeStartContext()
        {
        }

        protected virtual void AfterBindingsBeforeInjections()
        {
        }

        protected virtual void AfterStarBeforeLaunchContext()
        {
        }

        public void BindSignals(bool forceToBind = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoBindInjections && !forceToBind)
                return;

            if (signalsBound)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Bind Signals!");
            Context.SignalBindings();
            signalsBound = true;
        }

        public void BindInjections(bool forceToBind = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoBindInjections && !forceToBind)
                return;

            if (injectionsBound)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Bind Injections!");
            Context.InjectionBindings();
            injectionsBound = true;
        }

        public void BindMediations(bool forceToBind = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoBindMediations && !forceToBind)
                return;

            if (mediationsBound)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Bind Mediations!");
            Context.MediationBindings();
            mediationsBound = true;
        }

        public void BindCommands(bool forceToBind = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoBindInjections && !forceToBind)
                return;

            if (commandsBound)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Bind Commands!");
            Context.CommandBindings();
            commandsBound = true;
        }

        public IContext GetContext()
        {
            return Context;
        }

        public Stack<IContext> GetSubContexts()
        {
            return new Stack<IContext>(_subContexts.Keys);
        }

        public Stack<IContext> GetAllContexts()
        {
            Stack<IContext> list = new Stack<IContext>(_subContexts.Keys);
            list.Push(GetContext());
            return list;
        }

        public void Setup(bool forceToSetup = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoSetup && !forceToSetup)
                return;

            if (hasSetuped)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Setup! ");
            Context.Setup();
            hasSetuped = true;

            foreach (KeyValuePair<IContext, SubContextData> subContext in _subContexts)
            {
                if (!subContext.Value.AutoSetup) continue;
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Value.ContextName + " | Setup!");
                subContext.Key.Setup();
            }
        }

        public virtual void Launch(bool forceToLaunch = false)
        {
            if (!hasInitialized)
                return;

            if (!AutoLaunch && !forceToLaunch)
                return;

            if (hasLaunched)
                return;

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Context Launch!");
            Context.Launch();
            hasLaunched = true;
        }
    }
}