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

        [HideInInspector] public int initializeOrder;

        [HideInInspector] internal bool signalsBound;
        [HideInInspector] internal bool injectionsBound;
        [HideInInspector] internal bool mediationsBound;
        [HideInInspector] internal bool commandsBound;
        [HideInInspector] internal bool hasInitialized;
        [HideInInspector] internal bool hasSetuped;
        [HideInInspector] internal bool hasLaunched;

        [HideInInspector] public bool AutoBindInjections = true;
        [HideInInspector] public bool AutoBindMediations = true;
        [HideInInspector] public bool AutoInitialize = true;
        [HideInInspector] public bool AutoSetup = true;
        [HideInInspector] public bool AutoLaunch = true;
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