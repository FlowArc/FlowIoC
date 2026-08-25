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

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | Started!");
            _context.Start();
            BindSignals();
            BindInjections();
            BindMediations();
            BindCommands();

            foreach (KeyValuePair<IContext, SubContextData> subContext in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Started!");
                subContext.Key.Start();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Bind Signals");
                subContext.Key.SignalBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Bind Injections");
                subContext.Key.InjectionBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Bind Mediations");
                subContext.Key.MediationBindings();

                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContext.Key.GetType().Name + " | Bind Commands!");
                subContext.Key.CommandBindings();
            }

            AfterBindingsBeforeInjections();

            FlowLogger.Log(SystemLogType.Context, _context.GetType().Name + " | InjectAllInstances!");
            _context.InjectAllInstances();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | InjectAllInstances!");
                subContextData.Key.InjectAllInstances();
            }

            FlowLogger.Log(SystemLogType.Context, _context.GetType().Name + " | Executed Post Construct Methods!");
            _context.ExecutePostConstructMethods();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | Executed Post Construct Methods!");
                subContextData.Key.ExecutePostConstructMethods();
            }

            AfterStarBeforeLaunchContext();

            _rootsManager.OnContextReady?.Invoke(Context);
        }

        protected virtual void DestroyContext()
        {
            _rootsManager.UnRegister(this);

            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | DestroyContext!");
            _context.DestroyContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | DestroyContext!");
                subContextData.Key.DestroyContext();
            }

            injectionsBound = false;
            mediationsBound = false;
            commandsBound = false;
        }

        protected virtual void PauseContext()
        {
            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | PauseContext!");
            _context.PauseContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | PauseContext!");
                subContextData.Key.PauseContext();
            }
        }

        protected virtual void ResumeContext()
        {
            FlowLogger.Log(SystemLogType.Context, GetType().Name + " | ResumeContext!");
            _context.ResumeContext();

            foreach (KeyValuePair<IContext, SubContextData> subContextData in _subContexts)
            {
                FlowLogger.Log(SystemLogType.Context, "Sub | " + subContextData.Key.GetType().Name + " | ResumeContext!");
                subContextData.Key.PauseContext();
            }
        }
    }
}