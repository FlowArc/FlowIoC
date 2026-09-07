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
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A screen test scene keeps the screen's prefab in the scene so it can be edited there, and
    /// the run opens the screen from code instead - so the authored instance has to go before
    /// anything registers it.
    ///
    /// When it goes is the whole question. These say what the hazard was, and that clearing in
    /// Awake removes it rather than undoing it afterwards.
    /// </summary>
    public class ScreenTestRootCleanupTests
    {
        private class ProbeScreen : ScreenBody
        {
        }

        private class ProbeScreenMediator : IMediator
        {
            public static int Registered;

            [Inject] private ProbeScreen _view { get; set; }

            public void OnRegister() => Registered++;

            public void OnRemove() { }
        }

        /// <summary>Edit mode runs no Unity messages, so the probe says when Awake happens.</summary>
        private class ProbeScreenTestRoot : BaseScreenTestRoot<BaseScreenContext>
        {
            internal void Build() =>
                typeof(Root<BaseScreenContext>)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(this, null);
        }

        private RootsManager _rootsManager;
        private InjectionBinderCrossContext _crossContext;
        private readonly List<GameObject> _hosts = new();

        [SetUp]
        public void SetUp()
        {
            ProbeScreenMediator.Registered = 0;

            _rootsManager = (RootsManager) RootsManagerFactory.GetRootsManager();
            _crossContext = _rootsManager.InjectionBinderCrossContext;
            _rootsManager.OnContextReady = null;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _rootsManager.OnContextReady = null;

            foreach (GameObject host in _hosts)
            {
                if (host != null)
                    Object.DestroyImmediate(host);
            }

            _hosts.Clear();

            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();

            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        private GameObject NewAuthoredScreen(out ViewInjector injector)
        {
            GameObject host = new GameObject("AuthoredScreen");
            _hosts.Add(host);

            injector = host.AddComponent<ViewInjector>();
            host.AddComponent<ProbeScreen>();
            injector.InitializeForEditor();

            return host;
        }

        /// <summary>
        /// The hazard, on its own. A screen left in the scene carries a ViewInjector, and Unity's
        /// Start order is not ours to choose: if the injector starts before the Root does, no
        /// context has started yet and it waits on OnContextReady - which StartContext raises a few
        /// lines after it would have destroyed the screen. So the instance that is on its way out
        /// gets its Mediator built and registered.
        ///
        /// This is what the hand-written ViewInjector.OnDestroy call was undoing, and it is why the
        /// clearing moved to Awake, where there is nothing to undo.
        /// </summary>
        [Test]
        public void An_injector_that_starts_before_the_context_registers_when_the_context_becomes_ready()
        {
            GameObject rootObject = new GameObject("ScreenTestRoot");
            _hosts.Add(rootObject);

            RootBase root = rootObject.AddComponent<RootBase>();
            Context context = new Context();
            context.Initialize(rootObject, 0, _crossContext, new List<IContext>());
            root.Context = context;
            _rootsManager.Register(root);

            GameObject screen = NewAuthoredScreen(out ViewInjector injector);
            screen.transform.SetParent(rootObject.transform);
            injector.GetViewInjectorData(screen.GetComponent<ProbeScreen>()).ContextSource = ViewContextSource.BubbleUp;

            context.Start();
            context.MediationBinder.Bind<ProbeScreen>().To<ProbeScreenMediator>();
            context.IsStarted = false;

            StartInjector(injector);

            Assert.That(ProbeScreenMediator.Registered, Is.Zero, "nothing has started yet");
            Assert.IsNotNull(_rootsManager.OnContextReady, "the injector is waiting");

            context.IsStarted = true;
            _rootsManager.OnContextReady(context);

            Assert.That(ProbeScreenMediator.Registered, Is.EqualTo(1),
                "the screen on its way out was registered, which is what had to be undone by hand");

            _rootsManager.UnRegister(root);
            context.DestroyContext();
        }

        /// <summary>
        /// The fix. Awake is before every Start, so the authored screen is out of reach of Unity's
        /// Start phase by the time any injector could run - deactivated first, because Destroy is
        /// deferred to the end of the frame and deactivating says so now.
        /// </summary>
        [Test]
        public void A_screen_test_root_takes_the_authored_screen_out_of_reach_in_Awake()
        {
            GameObject screen = NewAuthoredScreen(out ViewInjector _);

            GameObject rootObject = new GameObject("ScreenTestRoot");
            _hosts.Add(rootObject);
            ProbeScreenTestRoot root = rootObject.AddComponent<ProbeScreenTestRoot>();

            // Destroy is refused in edit mode and says so; the deactivation is the half that can be
            // asserted here, and it is the half the guarantee rests on.
            LogAssert.ignoreFailingMessages = true;

            root.Build();

            Assert.IsFalse(screen.activeSelf, "an inactive object never reaches Start");
            Assert.IsNotNull(root.Context, "and the context was built after the scene was cleared");

            _rootsManager.UnRegister(root);
            root.Context.DestroyContext();
        }

        /// <summary>
        /// The consequence: nothing is left subscribed. The clearing used to happen in the middle
        /// of the Start phase, so an injector could already be waiting on OnContextReady by then.
        /// </summary>
        [Test]
        public void Nothing_is_waiting_on_a_context_after_a_screen_test_root_wakes()
        {
            NewAuthoredScreen(out ViewInjector _);

            GameObject rootObject = new GameObject("ScreenTestRoot");
            _hosts.Add(rootObject);
            ProbeScreenTestRoot root = rootObject.AddComponent<ProbeScreenTestRoot>();

            LogAssert.ignoreFailingMessages = true;

            root.Build();

            Assert.IsNull(_rootsManager.OnContextReady);
            Assert.That(ProbeScreenMediator.Registered, Is.Zero);

            _rootsManager.UnRegister(root);
            root.Context.DestroyContext();
        }

        /// <summary>
        /// Start is a Unity message and an edit mode test is not a running scene, so it is called
        /// by name - the only way to exercise what the injector does before its context is ready.
        /// </summary>
        private static void StartInjector(ViewInjector injector) =>
            typeof(ViewInjector)
                .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(injector, null);
    }
}
