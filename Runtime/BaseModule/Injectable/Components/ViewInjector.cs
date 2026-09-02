using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
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

        /// <summary>
        /// Names the context this object's views belong to, for a view that is not authored under
        /// its module's Root - a screen the screen service instantiates and parents under a layer,
        /// for example. Set it before the object is activated; every view on the object then
        /// registers against this context instead of the one found by bubbling up the hierarchy.
        /// </summary>
        public void AssignContext(IContext context) => _assignedContext = context;

        internal IContext AssignedContext => _assignedContext;

        #region Unity Methods

        private void Start()
        {
            _viewRegistrationDataDict = new Dictionary<IView, IContext>(viewDataList.Count);

            IContext context = null;
            _rootsManager = RootsManagerFactory.GetRootsManager() as RootsManager;

            for (int i = 0; i < viewDataList.Count; i++)
            {
                ViewInjectorData viewInjectorData = viewDataList[i];

                if (_assignedContext != null)
                    context = _assignedContext;
                else if (viewInjectorData.UseBubbleUp)
                    context = (transform.GetComponent<IView>()).FindViewContext();
                else if (viewInjectorData.UseRootSelection)
                    context = viewInjectorData.SelectedRoot.GetContext();
                else
                    context = _rootsManager.GetRootByName(viewInjectorData.RootName).GetContext();

                _viewRegistrationDataDict.Add((IView) viewInjectorData.View, context);

                //if (_rootsManager.IsContextReady(context))
                if (context.IsStarted)
                {
                    RegisterView(viewInjectorData);
                }
                else
                {
                    _rootsManager.OnContextReady += OnContextsReadyListener;
                }
            }
        }

        public virtual void OnDestroy()
        {
            RootsManagerFactory.ExecuteSafelyOnRootsManager(rootsManager =>
            {
                if (_rootsManager?.OnContextReady != null)
                {
                    _rootsManager.OnContextReady -= OnContextsReadyListener;
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

        #region Injection

        private void OnContextsReadyListener(IContext context)
        {
            bool foundAny = false;
            for (int i = 0; i < viewDataList.Count; i++)
            {
                if (!_viewRegistrationDataDict.TryGetValue((IView) viewDataList[i].View, out var registeredContext)
                    || registeredContext != context) continue;
                RegisterView(viewDataList[i]);
                foundAny = true;
            }

            if (foundAny)
            {
                _rootsManager.OnContextReady -= OnContextsReadyListener;
            }
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