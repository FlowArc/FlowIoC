using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Bind.Binders;
using FlowIoC.BaseModule.Bind.Bindings.Mediator;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Injectable.Mediator;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Injectable.Binders
{
    [HideInModelViewer]
    public class MediationBinder : Binder<MediatorBinding>
    {
        private readonly Dictionary<IView, InjectedMediatorData> _injectedMediators;
        private readonly MediatorCreatorController _mediatorCreatorController;


        public MediationBinder()
        {
            _injectedMediators = new Dictionary<IView, InjectedMediatorData>();
            RootsManager rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;
            _mediatorCreatorController = rootsManager?.MediatorCreatorController;
        }
        public new virtual MediatorBinding Bind<TKeyType>()
            where TKeyType : IView
        {
            return base.Bind<TKeyType>();
        }

        public override MediatorBinding Bind(object key)
        {
            if (key is not IView)
            {
                string viewTypeName = key?.GetType().Name ?? "Unknown";
                FlowLogger.LogError(SystemLogType.Injection, "Binding View requires inheriting from IMVCView interface! " + viewTypeName);
                return null;
            }

            return base.Bind(key);
        }

        internal IMediator GetMediatorFromPool(Type mediatorType)
        {
            return _mediatorCreatorController.GetMediator(mediatorType);
        }

        internal void SendMediatorToPool(IMediator mediator)
        {
            _mediatorCreatorController.ReturnMediatorToPool(mediator);
        }

        internal InjectedMediatorData GetOrCreateInjectedMediatorData(IView view)
        {
            if (!_injectedMediators.TryGetValue(view, out var mediatorData))
            {
                mediatorData = new InjectedMediatorData
                {
                    view = view,
                    viewInjector = view.transform.GetComponent<ViewInjector>()
                };
                _injectedMediators[view] = mediatorData;
            }

            return mediatorData;
        }

        internal InjectedMediatorData GetInjectedMediatorData(IView view)
        {
            _injectedMediators.TryGetValue(view, out var mediatorData);
            return mediatorData;
        }
    }
}