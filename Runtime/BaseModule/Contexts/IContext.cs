using System.Collections.Generic;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.CrossContext;
using UnityEngine;

namespace FlowIoC.BaseModule.Contexts
{
    public interface IContext
    {
        List<IContext> SubContexts { get; set; }
        List<IContext> AllContexts { get; set; }

        bool IsTest { get; set; }
        int InitializeOrder { get; set; }
        bool IsStarted { get; set; }
        MediationBinder MediationBinder { get; set; }
        InjectionBinder InjectionBinder { get; set; }
        InjectionBinderCrossContext InjectionBinderCrossContext { get; set; }
        ICommandBinder CommandBinder { get; set; }
        void Initialize(GameObject contextGameObject, int initializeOrder, InjectionBinderCrossContext injectionBinderCrossContext, List<IContext> subContexts, bool isTest = false);
        void Start();

        internal void InjectAllInstances();
        internal void ExecutePostConstructMethods();
        void SignalBindings();
        void InjectionBindings();
        void MediationBindings();
        void CommandBindings();
        void Setup();
        void Launch();
        void DestroyContext();
        void PauseContext();
        void ResumeContext();

    }
}