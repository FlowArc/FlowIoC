using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Which Context a view registers against. The three sources are alternatives, and only the
    /// injector decides between them: registration used to look at the selected Root alone, so a
    /// view that named its Root waited for the named context and then registered against whatever
    /// Root happened to sit above it.
    /// </summary>
    public class ViewInjectorContextSourceTests
    {
        private class ProbeView : MonoBehaviour, IView
        {
            public bool IsRegistered { get; set; }
        }

        private class SecondProbeView : MonoBehaviour, IView
        {
            public bool IsRegistered { get; set; }
        }

        private class ProbeMediator : IMediator
        {
            public static int Registered;

            [Inject] private ProbeView _view { get; set; }

            public void OnRegister() => Registered++;

            public void OnRemove() { }
        }

        private class SecondProbeMediator : IMediator
        {
            public static int Registered;

            [Inject] private SecondProbeView _view { get; set; }

            public void OnRegister() => Registered++;

            public void OnRemove() { }
        }

        private RootsManager _rootsManager;
        private InjectionBinderCrossContext _crossContext;

        private GameObject _nearRootObject;
        private GameObject _farRootObject;
        private RootBase _nearRoot;
        private RootBase _farRoot;
        private Context _nearContext;
        private Context _farContext;

        private GameObject _viewHost;
        private ViewInjector _injector;
        private ProbeView _view;

        [SetUp]
        public void SetUp()
        {
            ProbeMediator.Registered = 0;
            SecondProbeMediator.Registered = 0;

            _rootsManager = (RootsManager) RootsManagerFactory.GetRootsManager();
            _crossContext = _rootsManager.InjectionBinderCrossContext;
            _rootsManager.OnContextReady = null;

            // Two Roots that know nothing of each other: the one the view sits under, and one
            // somewhere else in the scene. Every test here is about telling them apart.
            _nearRootObject = new GameObject("NearRoot");
            _nearRoot = _nearRootObject.AddComponent<RootBase>();
            _nearContext = NewContext(_nearRootObject);
            _nearRoot.Context = _nearContext;
            _rootsManager.Register(_nearRoot);

            _farRootObject = new GameObject("FarRoot");
            _farRoot = _farRootObject.AddComponent<RootBase>();
            _farContext = NewContext(_farRootObject);
            _farRoot.Context = _farContext;
            _rootsManager.Register(_farRoot);

            // Only the far context knows how to mediate the view, so a registration that lands on
            // the near one fails and says so - which is the whole point of these tests.
            _farContext.MediationBinder.Bind<ProbeView>().To<ProbeMediator>();

            _viewHost = new GameObject("ProbeView");
            _viewHost.transform.SetParent(_nearRootObject.transform);
            _injector = _viewHost.AddComponent<ViewInjector>();
            _view = _viewHost.AddComponent<ProbeView>();
            _injector.InitializeForEditor();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            _rootsManager.OnContextReady = null;
            _rootsManager.UnRegister(_nearRoot);
            _rootsManager.UnRegister(_farRoot);

            _nearContext.DestroyContext();
            _farContext.DestroyContext();

            Object.DestroyImmediate(_viewHost);
            Object.DestroyImmediate(_nearRootObject);
            Object.DestroyImmediate(_farRootObject);

            // Context.Start binds the two providers as scene objects into the shared cross-context
            // binder; the next test's Start would otherwise be handed the destroyed ones.
            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();
            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        private Context NewContext(GameObject host)
        {
            var context = new Context();
            context.Initialize(host, 0, _crossContext, new List<IContext>());
            context.Start();

            return context;
        }

        private ViewInjectorData Entry => _injector.GetViewInjectorData(_view);

        #region Resolving

        [Test]
        public void Bubble_up_finds_the_Root_above_the_view()
        {
            Entry.ContextSource = ViewContextSource.BubbleUp;

            Assert.AreSame(_nearContext, _injector.ResolveContext(Entry));
        }

        [Test]
        public void A_selected_Root_is_used_instead_of_the_one_above()
        {
            Entry.ContextSource = ViewContextSource.SelectedRoot;
            Entry.SelectedRoot = _farRoot;

            Assert.AreSame(_farContext, _injector.ResolveContext(Entry));
        }

        [Test]
        public void A_named_Root_is_used_instead_of_the_one_above()
        {
            Entry.ContextSource = ViewContextSource.RootName;
            Entry.RootName = _farRootObject.name;

            Assert.AreSame(_farContext, _injector.ResolveContext(Entry));
        }

        [Test]
        public void A_name_no_Root_answers_to_is_reported_rather_than_thrown()
        {
            Entry.ContextSource = ViewContextSource.RootName;
            Entry.RootName = "NoSuchRoot";

            LogAssert.ignoreFailingMessages = true;

            Assert.IsNull(_injector.ResolveContext(Entry));
        }

        [Test]
        public void A_selected_Root_that_was_never_set_is_reported_rather_than_thrown()
        {
            Entry.ContextSource = ViewContextSource.SelectedRoot;
            Entry.SelectedRoot = null;

            LogAssert.ignoreFailingMessages = true;

            Assert.IsNull(_injector.ResolveContext(Entry));
        }

        [Test]
        public void An_assigned_context_outranks_every_source()
        {
            Entry.ContextSource = ViewContextSource.RootName;
            Entry.RootName = _farRootObject.name;
            _injector.AssignContext(_nearContext);

            Assert.AreSame(_nearContext, _injector.ResolveContext(Entry));
        }

        [Test]
        public void An_unknown_name_answers_null_rather_than_throwing()
        {
            Assert.IsNull(_rootsManager.GetRootByName("NoSuchRoot"));
        }

        #endregion

        #region Registering

        [Test]
        public void A_view_registers_against_the_Root_it_names()
        {
            Entry.ContextSource = ViewContextSource.RootName;
            Entry.RootName = _farRootObject.name;

            Assert.IsTrue(_view.Register());
            Assert.AreEqual(1, ProbeMediator.Registered);
        }

        [Test]
        public void A_view_registers_against_the_Root_it_selects()
        {
            Entry.ContextSource = ViewContextSource.SelectedRoot;
            Entry.SelectedRoot = _farRoot;

            Assert.IsTrue(_view.Register());
            Assert.AreEqual(1, ProbeMediator.Registered);
        }

        [Test]
        public void Bubbling_up_to_a_Root_that_does_not_mediate_the_view_registers_nothing()
        {
            Entry.ContextSource = ViewContextSource.BubbleUp;

            LogAssert.ignoreFailingMessages = true;

            Assert.IsFalse(_view.Register());
            Assert.AreEqual(0, ProbeMediator.Registered);
        }

        /// <summary>
        /// Auto Register says whether the injector registers the view on its own. Pressing the
        /// inspector's own Register button is by definition not on its own, so it still works.
        /// </summary>
        [Test]
        public void A_view_that_does_not_register_itself_can_still_be_registered_by_hand()
        {
            Entry.ContextSource = ViewContextSource.SelectedRoot;
            Entry.SelectedRoot = _farRoot;
            Entry.AutoRegister = false;

            Assert.IsTrue(_view.Register());
            Assert.AreEqual(1, ProbeMediator.Registered);
        }

        #endregion

        #region Waiting for contexts

        /// <summary>
        /// Two views on one object, each waiting for a different Root. The injector subscribed
        /// once per view and unsubscribed once per handler run, so the first Root to become ready
        /// took away the subscription the second view was still waiting on.
        /// </summary>
        [Test]
        public void One_context_becoming_ready_does_not_strand_a_view_waiting_for_another()
        {
            _farContext.MediationBinder.Bind<SecondProbeView>().To<SecondProbeMediator>();

            var secondView = _viewHost.AddComponent<SecondProbeView>();
            _injector.InitializeForEditor();

            ViewInjectorData first = _injector.GetViewInjectorData(_view);
            first.ContextSource = ViewContextSource.SelectedRoot;
            first.SelectedRoot = _farRoot;

            ViewInjectorData second = _injector.GetViewInjectorData(secondView);
            second.ContextSource = ViewContextSource.SelectedRoot;
            second.SelectedRoot = _nearRoot;

            // The near context is the one that mediates the second view, and neither context has
            // started yet: that is what the injector waits on.
            _nearContext.MediationBinder.Bind<SecondProbeView>().To<SecondProbeMediator>();
            _farContext.IsStarted = false;
            _nearContext.IsStarted = false;

            StartInjector();

            _farContext.IsStarted = true;
            _rootsManager.OnContextReady(_farContext);

            Assert.AreEqual(1, ProbeMediator.Registered, "the view waiting for the far Root should have registered");
            Assert.AreEqual(0, SecondProbeMediator.Registered);
            Assert.IsNotNull(_rootsManager.OnContextReady, "the injector is still waiting for the near Root");

            _nearContext.IsStarted = true;
            _rootsManager.OnContextReady(_nearContext);

            Assert.AreEqual(1, SecondProbeMediator.Registered);
            Assert.IsNull(_rootsManager.OnContextReady, "nothing is left to wait for");
        }

        /// <summary>
        /// Start is a Unity message, and an edit mode test is not a running scene. Calling it is
        /// the only way to exercise what the injector does when its contexts are not ready yet.
        /// </summary>
        private void StartInjector()
        {
            MethodInfo start = typeof(ViewInjector).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

            start.Invoke(_injector, null);
        }

        #endregion
    }
}
