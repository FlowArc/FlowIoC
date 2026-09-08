using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Root
{
    public class Root<TContextType> : RootBase
        where TContextType : IContext, new()
    {
        protected TContextType _context
        {
            get => (TContextType) Context;
            private set => Context = value;
        }

        #region UnityMethods

        private void Awake()
        {
            if (_context != null)
                return;

            _rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;
            CreateContext();
            _rootsManager.Register(this);
        }

        private void Start()
        {
            if (_context == null)
                return;

            if (!_context.IsStarted)
                _rootsManager.StartContexts();
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;

            DestroyContext();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                PauseContext();
            else
                ResumeContext();
        }

        #endregion

        private void CreateContext()
        {
            BeforeCreateContext();
            InitializeSubContexts();
            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Initialize!");
            _context = new TContextType();
            _context.Initialize(gameObject, initializeOrder, _rootsManager.InjectionBinderCrossContext, _subContexts.Keys.ToList(), IsTest);
        }

        public override void StartContext(bool forceToStart = false)
        {
            if (_context == null)
                return;

            if (!AutoInitialize && !forceToStart)
                return;

            hasInitialized = true;
            AfterCreateBeforeStartContext();

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | started");
            _context.Start();
            BindSignals();
            BindInjections();
            BindMediations();
            BindCommands();

            foreach (KeyValuePair<IContext, SubContextData> subContext in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Started!");
                subContext.Key.Start();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | signals bound");
                subContext.Key.SignalBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | injections bound");
                subContext.Key.InjectionBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | mediations bound");
                subContext.Key.MediationBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | commands bound");
                subContext.Key.CommandBindings();
            }

            AfterBindingsBeforeInjections();

            FlowLogger.Log(SystemLogType.Context, _context.GetType().Name + " | injections resolved");
            _context.InjectAllInstances();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | InjectAllInstances!");
                subContextData.Key.InjectAllInstances();
            }

            FlowLogger.Log(SystemLogType.Context, _context.GetType().Name + " | post construct Methods!");
            _context.ExecutePostConstructMethods();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | Executed Post Construct Methods!");
                subContextData.Key.ExecutePostConstructMethods();
            }

            AfterStartBeforeLaunchContext();

            _rootsManager.OnContextReady?.Invoke(Context);
        }

        protected virtual void DestroyContext()
        {
            _rootsManager.UnRegister(this);

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | destroyed");
            _context.DestroyContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | DestroyContext!");
                subContextData.Key.DestroyContext();
            }

            // All of them, not three of the seven. A Root that has torn its context down has not
            // bound anything and has not been through any phase, and saying so in half the flags
            // left the other half claiming otherwise.
            signalsBound = false;
            injectionsBound = false;
            mediationsBound = false;
            commandsBound = false;
            hasInitialized = false;
            hasSetUp = false;
            hasLaunched = false;
        }

        protected virtual void PauseContext()
        {
            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | paused");
            _context.PauseContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | PauseContext!");
                subContextData.Key.PauseContext();
            }
        }

        protected virtual void ResumeContext()
        {
            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | resumed");
            _context.ResumeContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | ResumeContext!");
                subContextData.Key.ResumeContext();
            }
        }
    }
}