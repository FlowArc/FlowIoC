using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Injectable.Components
{
    /// <summary>
    /// Wears the Mediator colour. A Mediator is not a component, so that colour would otherwise
    /// never appear in an inspector - and this is the piece that hands a View to one.
    /// </summary>
    [FlowHeader(FlowRole.Mediator, label: "View Injector")]
    public class ViewInjector : MonoBehaviour
    {
        public List<ViewInjectorData> viewDataList;

        private Dictionary<IView, IContext> _viewRegistrationDataDict;

        private RootsManager _rootsManager;

        private IContext _assignedContext;

        private bool _waitingForContexts;

        /// <summary>
        /// Names the context this object's views belong to, for a view that is not authored under
        /// its module's Root - a screen the screen service instantiates and parents under a layer,
        /// for example. Set it before the object is activated; every view on the object then
        /// registers against this context instead of the one found by bubbling up the hierarchy.
        /// </summary>
        public void AssignContext(IContext context) => _assignedContext = context;

        internal IContext AssignedContext => _assignedContext;

        private RootsManager _rootsManagerInstance =>
            _rootsManager ??= RootsManagerFactory.GetRootsManager() as RootsManager;

        #region Unity Methods

        private void Start()
        {
            // An object built from code has never had this list written, and Unity hands a
            // component added at runtime a null one rather than an empty one.
            viewDataList ??= new List<ViewInjectorData>();

            if (viewDataList.Count == 0)
            {
                ReportUnfilled();

                return;
            }

            _viewRegistrationDataDict = new Dictionary<IView, IContext>(viewDataList.Count);

            bool waiting = false;

            for (int i = 0; i < viewDataList.Count; i++)
            {
                ViewInjectorData viewInjectorData = viewDataList[i];
                IContext context = ResolveContext(viewInjectorData);

                // ResolveContext has already said what was missing. Carrying on leaves the other
                // views on this object working, which is what a reader trying to fix one wants.
                if (context == null)
                    continue;

                _viewRegistrationDataDict[(IView) viewInjectorData.View] = context;

                if (context.IsStarted)
                    RegisterView(viewInjectorData);
                else
                    waiting = true;
            }

            // One subscription for the whole object. Subscribing once per view meant the handler
            // ran once per view and unsubscribed once per run, so the first context to become
            // ready could take away the subscription every other view was still waiting on.
            if (!waiting)
                return;

            _rootsManagerInstance.OnContextReady += OnContextsReadyListener;
            _waitingForContexts = true;
        }

        public virtual void OnDestroy()
        {
            RootsManagerFactory.ExecuteSafelyOnRootsManager(rootsManager =>
            {
                if (_waitingForContexts && _rootsManager?.OnContextReady != null)
                {
                    _rootsManager.OnContextReady -= OnContextsReadyListener;
                    _waitingForContexts = false;
                }
            });

            for (int i = 0; i < viewDataList.Count; i++)
            {
                ViewInjectorData viewInjectorData = viewDataList[i];
                if (!viewInjectorData.IsRegistered)
                    continue;
                IView view = (IView) viewInjectorData.View;
                view.UnRegister();
            }
        }

        /// <summary>
        /// The context a view was registered against. A screen registers before Unity's Start has
        /// built the dictionary, so the assigned context is answered first.
        /// </summary>
        public IContext GetContextOfView(IView view)
        {
            if (_assignedContext != null)
                return _assignedContext;

            return _viewRegistrationDataDict != null && _viewRegistrationDataDict.TryGetValue(view, out IContext context)
                ? context
                : null;
        }

        #endregion

        #region Context

        /// <summary>
        /// Which context a view belongs to, and the one place that answers it. Registration used
        /// to decide for itself and only ever looked at the selected Root, so a view that named
        /// its Root waited for the named context and then registered against whatever Root
        /// happened to sit above it in the hierarchy.
        ///
        /// A context assigned by a loader outranks everything the entry says, because the object
        /// cannot know in the editor where it will be parented.
        /// </summary>
        internal IContext ResolveContext(ViewInjectorData viewInjectorData)
        {
            if (_assignedContext != null)
                return _assignedContext;

            switch (viewInjectorData.ContextSource)
            {
                case ViewContextSource.SelectedRoot:
                    return ContextOfSelectedRoot(viewInjectorData);

                case ViewContextSource.RootName:
                    return ContextOfNamedRoot(viewInjectorData);

                default:
                    return ((IView) viewInjectorData.View).FindViewContext();
            }
        }

        private IContext ContextOfSelectedRoot(ViewInjectorData viewInjectorData)
        {
            if (viewInjectorData.SelectedRoot != null)
                return viewInjectorData.SelectedRoot.GetContext();

            Report(viewInjectorData, "is set to Selected Root and none is selected. A prefab cannot hold "
                                     + "a reference to a Root in the scene - name the Root instead.");

            return null;
        }

        private IContext ContextOfNamedRoot(ViewInjectorData viewInjectorData)
        {
            IRoot root = string.IsNullOrEmpty(viewInjectorData.RootName)
                ? null
                : _rootsManagerInstance.GetRootByName(viewInjectorData.RootName);

            if (root != null)
                return root.GetContext();

            Report(viewInjectorData, $"asks for the Root named \"{viewInjectorData.RootName}\", which is "
                                     + "not in the scene.");

            return null;
        }

        /// <summary>
        /// Says which view on which object could not find a context. Both halves matter: an
        /// object carries several views, and a scene carries several of the object.
        /// </summary>
        private void Report(ViewInjectorData viewInjectorData, string problem)
        {
            string viewName = viewInjectorData.View == null ? "a missing view" : viewInjectorData.View.GetType().Name;
            string message = $"ViewInjector on \"{name}\": {viewName} {problem}";

            FlowLogger.LogError(SystemLogType.Injection, message);
        }

        /// <summary>
        /// The list is filled by the injector's own inspector, so an object assembled from code
        /// reaches Start with nothing in it and every view on the object stays unregistered without
        /// a word. An injector on an object that carries no view is idle rather than broken, so
        /// that one says nothing.
        /// </summary>
        private void ReportUnfilled()
        {
            List<IView> views = new List<IView>();
            GetComponents(views);

            if (views.Count == 0)
                return;

            string[] viewNames = new string[views.Count];
            for (int i = 0; i < views.Count; i++)
                viewNames[i] = views[i].GetType().Name;

            string message = $"ViewInjector on \"{name}\": the view list is empty, so no view on this object "
                             + $"registers: {string.Join(", ", viewNames)}. The list is filled by the injector's "
                             + "own inspector - select the object once and save the scene.";

            FlowLogger.LogError(SystemLogType.Injection, message);
        }

        #endregion

        #region Injection

        private void OnContextsReadyListener(IContext context)
        {
            bool pending = false;

            for (int i = 0; i < viewDataList.Count; i++)
            {
                ViewInjectorData viewInjectorData = viewDataList[i];

                if (!_viewRegistrationDataDict.TryGetValue((IView) viewInjectorData.View, out IContext registeredContext))
                    continue;

                if (registeredContext == context)
                    RegisterView(viewInjectorData);
                else if (!registeredContext.IsStarted)
                    pending = true;
            }

            // Held until every view on this object has its context. One Root becoming ready says
            // nothing about the Root another view on the same object is waiting for.
            if (pending)
                return;

            _rootsManagerInstance.OnContextReady -= OnContextsReadyListener;
            _waitingForContexts = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterView(ViewInjectorData viewInjectorData)
        {
            if (viewInjectorData.AutoRegister)
                TryToInject((IView) viewInjectorData.View);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryToInject(IView viewComponent)
        {
            ViewInjectorData injectorData = GetViewInjectorData(viewComponent);
            if (injectorData.IsRegistered)
                return false;

            bool injectResult = viewComponent.Register(injectorData);
            injectorData.IsRegistered = injectResult;
            return injectResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ViewInjectorData GetViewInjectorData(IView view)
        {
            for (int i = 0; i < viewDataList.Count; i++)
            {
                if (viewDataList[i].View == (Object) view)
                    return viewDataList[i];
            }

            return null;
        }

        public void ViewInjectionCompleted(IView view)
        {
            ViewInjectorData injectorData = GetViewInjectorData(view);
            if (injectorData == null)
                return;

            injectorData.IsRegistered = true;
            view.IsRegistered = true;
        }

        #endregion

        internal void InitializeForEditor()
        {
            viewDataList = new List<ViewInjectorData>();

            List<IView> viewComponentList = new List<IView>();
            GetComponents<IView>(viewComponentList);

            for (int i = 0; i < viewComponentList.Count; i++)
            {
                ViewInjectorData viewInjectorData = new ViewInjectorData
                {
                    View = (Object) viewComponentList[i],
                    AutoRegister = true,
                    IsRegistered = false
                };

                viewDataList.Add(viewInjectorData);
            }
        }
    }
}