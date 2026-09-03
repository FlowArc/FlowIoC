using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Bind.Bindings.Mediator;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Injectable.Mediator;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.ConsoleModule;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.BaseModule.ViewsMediators.Utils
{
    public static class ViewExtensions
    {
        public static bool Register(this IView view)
        {
            ViewInjector injector = view.transform.GetComponent<ViewInjector>();
            ViewInjectorData injectorData = injector == null ? null : injector.GetViewInjectorData(view);

            return injectorData == null
                ? view.Register(view.AssignedContext() ?? view.FindViewContext())
                : view.Register(injectorData);
        }

        internal static bool Register(this IView view, ViewInjectorData injectorData)
        {
            ViewInjector injector = view.transform.GetComponent<ViewInjector>();

            // The injector decides where a view belongs. This used to look at the selected Root
            // alone, so the two other ways of naming a context were resolved when the object
            // started and then thrown away here.
            return view.Register(injector == null ? null : injector.ResolveContext(injectorData));
        }

        private static bool Register(this IView view, IContext context)
        {
            if (view.IsRegistered)
            {
                Debug.LogWarning("View is already registered. \nviewType: " + view.GetType().Name);
                FlowLogger.LogWarning(SystemLogType.Injection, "View is already registered. \nviewType: " + view.GetType().Name);
                return false;
            }

            if (context != null)
                return view.RegisterIn(context);

            FlowLogger.LogError(SystemLogType.Injection, "There is no Context \nviewType: " + view.GetType().Name);

            return false;
        }

        /// <summary>
        /// The registration itself, once the context is known. Auto Register is not consulted
        /// here: it says whether the injector registers the view on its own, and the inspector's
        /// own Register button is by definition not on its own.
        /// </summary>
        private static bool RegisterIn(this IView view, IContext context)
        {
            ViewBindingData viewBindingData = context.GetBindingData(view);
            if (viewBindingData.Equals(default))
            {
                FlowLogger.LogError(SystemLogType.Injection, "There is no view binding! " + view.GetType());
                return false;
            }

            if (viewBindingData.Context == null)
            {
                FlowLogger.LogError(SystemLogType.Injection, "There is no Context \nviewType: " + view.GetType().Name);
                return false;
            }

            ViewInjector viewInjector = view.transform.GetComponent<ViewInjector>();
            ViewInjectorData viewInjectionData = viewInjector.GetViewInjectorData(view);

            if (viewInjectionData.InjectableView)
                context.TryToInjectObject(view);

            MediationBinder mediationBinder = viewBindingData.Context.MediationBinder;
            Type mediatorType = viewBindingData.Binding.Value as Type;
            bool mediatorIsMono = mediatorType != null && mediatorType.IsSubclassOf(typeof(Object));

            IMediator mediator;

            if (mediatorIsMono)
                mediator = view.gameObject.AddComponent(mediatorType) as IMediator;
            else
                mediator = mediationBinder.GetMediatorFromPool(mediatorType);

            bool injectionResult = context.TryToInjectMediator(mediator, view);
            if (injectionResult)
            {
                InjectedMediatorData injectedMediatorData = mediationBinder.GetOrCreateInjectedMediatorData(view);
                if (injectedMediatorData == null)
                {
                    Debug.LogError("Injection Data not found! ", view.gameObject);
                    FlowLogger.LogError(SystemLogType.Injection, "Injection Data not found! ");
                    return false;
                }

                injectedMediatorData.viewInjector.ViewInjectionCompleted(view);
                injectedMediatorData.mediator = mediator;
            }

            return injectionResult;
        }

        private static bool FindMediationBinder(this IView view, out MediationBinder mediationBinder)
        {
            IContext viewContext = view.AssignedContext() ?? view.FindViewContext();
            mediationBinder = null;

            if (viewContext == null)
                return false;

            mediationBinder = viewContext.MediationBinder;
            if (mediationBinder == null)
                return false;

            InjectedMediatorData injectedMediatorData = mediationBinder.GetInjectedMediatorData(view);
            if (injectedMediatorData?.mediator != null)
                return true;

            List<IContext> viewSubContexts = viewContext.SubContexts;
            foreach (IContext viewSubContext in viewSubContexts)
            {
                mediationBinder = viewSubContext.MediationBinder;
                if (mediationBinder == null)
                    return false;

                injectedMediatorData = mediationBinder.GetInjectedMediatorData(view);
                if (injectedMediatorData?.mediator != null)
                    return true;
            }

            return false;
        }

        public static void UnRegister(this IView view)
        {
            if (!view.FindMediationBinder(out var mediationBinder))
                return;

            InjectedMediatorData injectedMediatorData = mediationBinder.GetInjectedMediatorData(view);

            ViewInjector viewInjectorComponent = injectedMediatorData.viewInjector;

            ViewInjectorData viewInjectorData = viewInjectorComponent.GetViewInjectorData(view);
            if (!viewInjectorData.IsRegistered)
                return;

            IMediator mediator = injectedMediatorData.mediator;
            mediator.OnRemove();

            view.IsRegistered = false;

            injectedMediatorData.mediator = null;
            viewInjectorData.IsRegistered = false;

            if (mediator is Object mediatorObject)
                Object.Destroy(mediatorObject as Component);
            else
                mediationBinder.SendMediatorToPool(mediator);
        }

        internal static IContext FindViewContext(this IView view)
        {
            IRoot contextRoot = view.FindRoot();
            return contextRoot == null ? null : contextRoot.GetContext();
        }

        /// <summary>
        /// The context the loader named on the injector, or null when the view is an ordinary one
        /// that finds its context by bubbling up.
        /// </summary>
        private static IContext AssignedContext(this IView view)
        {
            ViewInjector injector = view.transform.GetComponent<ViewInjector>();
            return injector == null ? null : injector.AssignedContext;
        }

        private static IRoot FindRoot(this IView view)
        {
            Transform parent = view.transform.parent;
            if (parent == null)
                return null;

            IRoot root = null;

            while (root == null)
            {
                root = parent.GetComponent<IRoot>();
                if (parent.parent == null)
                    break;
                parent = parent.parent;
            }

            return root;
        }

        internal static bool IsMediatorMono(this IMediator mediator)
        {
            return mediator is Object;
        }

        private static ViewBindingData GetBindingData(this IContext mainContext, IView view)
        {
            Type viewType = view.GetType();

            List<IContext> allContexts = mainContext.AllContexts;

            foreach (IContext context in allContexts)
            {
                ViewBindingData viewBindingData = new ViewBindingData();

                MediationBinder mediationBinder = context.MediationBinder;
                MediatorBinding binding = mediationBinder.GetBinding(viewType);
                if (binding == null) continue;
                viewBindingData.Binding = binding;
                viewBindingData.Context = context;
                return viewBindingData;
            }

            return default;
        }
    }
}