using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A context that answers the few questions the command engine and the function provider ask of
    /// one, and does nothing else. A real Context builds providers as GameObjects and binds them
    /// across the whole run, which a test of either of those does not need and cannot easily undo.
    ///
    /// Its injection binder is real, so a test that wants a command or a function to be handed
    /// something can bind it here first.
    /// </summary>
    internal class StandInContext : IContext
    {
        public List<IContext> SubContexts { get; set; } = new();
        public List<IContext> AllContexts { get; set; }
        public bool IsTest { get; set; }
        public int InitializeOrder { get; set; }
        public bool IsStarted { get; set; }
        public MediationBinder MediationBinder { get; set; }
        public InjectionBinder InjectionBinder { get; set; }
        public InjectionBinderCrossContext InjectionBinderCrossContext { get; set; }
        public ICommandBinder CommandBinder { get; set; }

        private readonly SignalParamResolver _signalParamResolver = new();

        SignalParamResolver IContext.SignalParamResolver => _signalParamResolver;

        public StandInContext()
        {
            AllContexts = new List<IContext> {this};

            InjectionBinder = new InjectionBinder();
            InjectionBinder.SetBindedContext(this);
        }

        public void Initialize(GameObject contextGameObject, int initializeOrder,
            InjectionBinderCrossContext injectionBinderCrossContext, List<IContext> subContexts, bool isTest = false) { }

        public void Start() { }
        void IContext.InjectAllInstances() { }
        void IContext.ExecutePostConstructMethods() { }
        public void SignalBindings() { }
        public void InjectionBindings() { }
        public void MediationBindings() { }
        public void CommandBindings() { }
        public void Setup() { }
        public void Launch() { }
        public void DestroyContext() { }
        public void PauseContext() { }
        public void ResumeContext() { }
    }
}
