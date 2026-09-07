using System.Collections.Generic;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using UnityEngine;

namespace FlowIoC.BaseModule.Contexts
{
    public interface IContext
    {
        List<IContext> SubContexts { get; }
        List<IContext> AllContexts { get; }

        bool IsTest { get; }
        int InitializeOrder { get; }
        bool IsStarted { get; set; }
        MediationBinder MediationBinder { get; }
        InjectionBinder InjectionBinder { get; }
        InjectionBinderCrossContext InjectionBinderCrossContext { get; }
        ICommandBinder CommandBinder { get; }
        void Initialize(GameObject contextGameObject, int initializeOrder, InjectionBinderCrossContext injectionBinderCrossContext, List<IContext> subContexts, bool isTest = false);
        void Start();

        internal void InjectAllInstances();
        internal void ExecutePostConstructMethods();
        internal SignalParamResolver SignalParamResolver { get; }
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