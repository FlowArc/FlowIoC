using System.Collections.Generic;
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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A screen is instantiated by the screen service and parented under a ScreenRoot layer, so
    /// bubbling up the hierarchy finds the wrong Root - or none. The loader names the owning
    /// context on the ViewInjector instead, and both registration and unregistration have to
    /// honour that name.
    /// </summary>
    public class ViewInjectorAssignedContextTests
    {
        private class ProbeView : MonoBehaviour, IView
        {
            public bool IsRegistered { get; set; }
        }

        private class ProbeMediator : IMediator
        {
            public static int Registered;
            public static int Removed;

            [Inject] private ProbeView _view { get; set; }

            public void OnRegister() => Registered++;

            public void OnRemove() => Removed++;
        }

        private GameObject _contextHost;
        private GameObject _viewHost;
        private Context _owner;
        private InjectionBinderCrossContext _crossContext;

        [SetUp]
        public void SetUp()
        {
            ProbeMediator.Registered = 0;
            ProbeMediator.Removed = 0;

            _crossContext = ((RootsManager) RootsManagerFactory.GetRootsManager()).InjectionBinderCrossContext;

            _contextHost = new GameObject("OwnerContextHost");
            _owner = new Context();
            _owner.Initialize(_contextHost, 0, _crossContext, new List<IContext>());
            _owner.Start();
            _owner.MediationBinder.Bind<ProbeView>().To<ProbeMediator>();

            // No Root anywhere above the view: bubbling up finds nothing, so only an assigned
            // context can register it. That is exactly a screen's situation.
            _viewHost = new GameObject("ProbeView");
            _viewHost.AddComponent<ViewInjector>();
            _viewHost.AddComponent<ProbeView>();
            _viewHost.GetComponent<ViewInjector>().InitializeForEditor();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _owner.DestroyContext();
            Object.DestroyImmediate(_viewHost);
            Object.DestroyImmediate(_contextHost);

            // Context.Start binds the two providers as scene objects into the shared cross-context
            // binder; the next test's Start would otherwise be handed the destroyed ones.
            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();
            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        [Test]
        public void A_view_registers_against_the_assigned_context()
        {
            ViewInjector injector = _viewHost.GetComponent<ViewInjector>();
            ProbeView view = _viewHost.GetComponent<ProbeView>();
            injector.AssignContext(_owner);

            bool registered = view.Register();

            Assert.IsTrue(registered);
            Assert.IsTrue(view.IsRegistered);
            Assert.AreEqual(1, ProbeMediator.Registered);
            Assert.AreSame(_owner, injector.GetContextOfView(view));
        }

        [Test]
        public void Unregister_finds_the_mediator_in_the_assigned_context()
        {
            ProbeView view = _viewHost.GetComponent<ProbeView>();
            _viewHost.GetComponent<ViewInjector>().AssignContext(_owner);
            view.Register();

            view.UnRegister();

            Assert.IsFalse(view.IsRegistered);
            Assert.AreEqual(1, ProbeMediator.Removed);
        }

        [Test]
        public void Without_an_assignment_a_view_with_no_root_above_it_is_not_registered()
        {
            ProbeView view = _viewHost.GetComponent<ProbeView>();

            // Register reports the missing context through Debug.LogError and, with ENABLE_LOG on,
            // through FlowLogger as well; the number of error lines is not the point here.
            LogAssert.ignoreFailingMessages = true;

            Assert.IsFalse(view.Register());
            Assert.AreEqual(0, ProbeMediator.Registered);
        }
    }
}
