using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.Signals;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A scene going away and coming back, driven through real Roots rather than through bare
    /// contexts. The Root is what a scene actually holds, and it is the Root that decides when a
    /// context is built, bound and torn down - so the question these ask is the one a reloaded
    /// scene asks: does the module come back working, and is what the old scene left behind
    /// properly deaf?
    ///
    /// Edit mode runs no Unity messages, so the probe Roots below say when Awake happens. Everything
    /// after that is the framework's own code.
    /// </summary>
    public class RootSceneReloadTests
    {
        internal static int Pings;
        internal static int Handled;

        #region The module under test

        internal class ReloadSignals : ISignalHolder
        {
            public Signal Ping = new(true);
        }

        public class PingCommand : Command
        {
            public override void Execute() => Pings++;
        }

        internal class ReloadContext : Context
        {
            private ReloadSignals _signals;

            public override void SignalBindings()
            {
                base.SignalBindings();
                _signals = InjectionBinderCrossContext.Bind<ReloadSignals>();
            }

            public override void CommandBindings()
            {
                base.CommandBindings();
                CommandBinder.Bind(_signals.Ping).ToSequence<PingCommand>();
            }

            public override void Launch()
            {
                base.Launch();
                _signals.Ping.Dispatch();
            }
        }

        #endregion

        #region Two modules and the connector between them

        internal class SourceSignals : ISignalHolder
        {
            public Signal Fired = new(true);
        }

        internal class TargetSignals : ISignalHolder
        {
            public Signal Handle = new(true);
        }

        public class HandledCommand : Command
        {
            public override void Execute() => Handled++;
        }

        internal class SourceContext : Context
        {
            public override void SignalBindings()
            {
                base.SignalBindings();
                InjectionBinderCrossContext.Bind<SourceSignals>();
            }
        }

        internal class TargetContext : Context
        {
            private TargetSignals _signals;

            public override void SignalBindings()
            {
                base.SignalBindings();
                _signals = InjectionBinderCrossContext.Bind<TargetSignals>();
            }

            public override void CommandBindings()
            {
                base.CommandBindings();
                CommandBinder.Bind(_signals.Handle).ToSequence<HandledCommand>();
            }
        }

        /// <summary>
        /// A Connector, in the shape the rules ask for: it gets both holders in Setup rather than
        /// binding either, and lets go of what it wired on the way out.
        /// </summary>
        internal class WiringContext : Context
        {
            private SourceSignals _source;
            private TargetSignals _target;

            public override void Setup()
            {
                base.Setup();

                _source = InjectionBinderCrossContext.GetInstance<SourceSignals>();
                _target = InjectionBinderCrossContext.GetInstance<TargetSignals>();

                _source.Fired.Connect(_target.Handle);
            }

            public override void DestroyContext()
            {
                _source?.Fired.Disconnect();
                _source = null;
                _target = null;

                base.DestroyContext();
            }
        }

        #endregion

        #region Probe roots

        private interface IProbeRoot
        {
            void Build();
            void TearDownNow();
        }

        /// <summary>
        /// The Root a scene holds, with the two things edit mode will not do for it: Awake, and a
        /// teardown that is safe to ask for twice, because the host object being destroyed asks for
        /// it as well.
        /// </summary>
        private class ProbeRoot<TContext> : Root<TContext>, IProbeRoot where TContext : IContext, new()
        {
            /// <summary>
            /// Edit mode sends no Unity messages and refuses SendMessage to a behaviour that does
            /// not run in it, so Awake is called by name. It is the Root's own Awake that runs -
            /// building the context and registering it - which is the point of driving the test
            /// through a Root at all.
            /// </summary>
            public void Build() =>
                typeof(Root<TContext>)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(this, null);

            /// <summary>Edit mode runs no OnDestroy either, so the scene going away is said here.</summary>
            public void TearDownNow() => DestroyContext();

            protected override void DestroyContext()
            {
                if (Context is not {IsStarted: true})
                    return;

                base.DestroyContext();
            }
        }

        private class ReloadRoot : ProbeRoot<ReloadContext>
        {
        }

        private class SourceRoot : ProbeRoot<SourceContext>
        {
        }

        private class TargetRoot : ProbeRoot<TargetContext>
        {
        }

        private class WiringRoot : ProbeRoot<WiringContext>
        {
        }

        #endregion

        private InjectionBinderCrossContext _crossContext;
        private readonly List<GameObject> _hosts = new();

        [SetUp]
        public void SetUp()
        {
            Pings = 0;
            Handled = 0;
            _crossContext = ((RootsManager) RootsManagerFactory.GetRootsManager()).InjectionBinderCrossContext;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in _hosts)
            {
                if (host == null)
                    continue;

                foreach (RootBase root in host.GetComponents<RootBase>())
                {
                    if (root is IProbeRoot probe)
                        probe.TearDownNow();
                }

                Object.DestroyImmediate(host);
            }

            _hosts.Clear();

            // The shared binder belongs to the run, so anything a failed assertion left on it is
            // taken off here rather than reaching the next test.
            _crossContext.UnBind<ReloadSignals>();
            _crossContext.UnBind<SourceSignals>();
            _crossContext.UnBind<TargetSignals>();

            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        /// <summary>
        /// A Root in a scene, built and bound. Setup and Launch are asked for by hand because the
        /// manager defers them to the end of the frame through a coroutine, and edit mode runs none.
        /// </summary>
        private TRoot StartRoot<TRoot>(string name, int order) where TRoot : RootBase, IProbeRoot
        {
            GameObject host = new GameObject(name);
            _hosts.Add(host);

            TRoot root = host.AddComponent<TRoot>();
            root.initializeOrder = order;
            root.Build();

            return root;
        }

        private static void Bind(RootBase root) => root.StartContext();

        /// <summary>
        /// The scene coming back. The module's holder is bound by its own context, so it goes with
        /// the Root and a new one takes its place - and the holder the old scene left behind must
        /// reach nothing, because the commands that answered it went with the same context.
        /// </summary>
        [Test]
        public void A_module_whose_root_went_and_came_back_binds_a_fresh_holder_and_runs_again()
        {
            ReloadRoot first = StartRoot<ReloadRoot>("ReloadRoot", 0);
            Bind(first);
            first.Setup();
            first.Launch();

            Assert.That(Pings, Is.EqualTo(1), "Launch dispatched the module's first signal");

            ReloadSignals before = _crossContext.GetInstance<ReloadSignals>();
            Assert.IsNotNull(before);

            first.TearDownNow();

            Assert.That(_crossContext.HasBinding<ReloadSignals>(), Is.False, "the holder went with its Root");

            before.Ping.Dispatch();
            Assert.That(Pings, Is.EqualTo(1), "the holder the old scene left behind reaches no command");

            ReloadRoot second = StartRoot<ReloadRoot>("ReloadRoot", 0);
            Bind(second);
            second.Setup();
            second.Launch();

            ReloadSignals after = _crossContext.GetInstance<ReloadSignals>();

            Assert.That(after, Is.Not.SameAs(before), "the rebuilt module bound a holder of its own");
            Assert.That(Pings, Is.EqualTo(2), "and the module works again");
        }

        /// <summary>
        /// The Connector is rebuilt with its scene like everything else, so after a reload it has to
        /// be wiring the holders the rebuilt modules bound rather than the ones it saw before. This
        /// is the case the teardown work was really about: the wiring is the one place that holds
        /// another module's holder, and holding a stale one is silent.
        /// </summary>
        [Test]
        public void A_connector_rebuilt_with_the_scene_wires_the_holders_the_new_modules_bound()
        {
            SourceRoot source = StartRoot<SourceRoot>("SourceRoot", 0);
            TargetRoot target = StartRoot<TargetRoot>("TargetRoot", 1);
            WiringRoot wiring = StartRoot<WiringRoot>("ConnectorRoot", 98);

            Bind(source);
            Bind(target);
            Bind(wiring);
            source.Setup();
            target.Setup();
            wiring.Setup();

            SourceSignals firstSource = _crossContext.GetInstance<SourceSignals>();
            firstSource.Fired.Dispatch();

            Assert.That(Handled, Is.EqualTo(1), "the connector joined the two modules");

            wiring.TearDownNow();
            target.TearDownNow();
            source.TearDownNow();

            firstSource.Fired.Dispatch();
            Assert.That(Handled, Is.EqualTo(1), "nothing the torn-down scene left behind still answers");

            SourceRoot sourceAgain = StartRoot<SourceRoot>("SourceRoot", 0);
            TargetRoot targetAgain = StartRoot<TargetRoot>("TargetRoot", 1);
            WiringRoot wiringAgain = StartRoot<WiringRoot>("ConnectorRoot", 98);

            Bind(sourceAgain);
            Bind(targetAgain);
            Bind(wiringAgain);
            sourceAgain.Setup();
            targetAgain.Setup();
            wiringAgain.Setup();

            SourceSignals secondSource = _crossContext.GetInstance<SourceSignals>();
            Assert.That(secondSource, Is.Not.SameAs(firstSource));

            secondSource.Fired.Dispatch();

            Assert.That(Handled, Is.EqualTo(2), "the rebuilt connector wired the rebuilt modules");

            firstSource.Fired.Dispatch();
            Assert.That(Handled, Is.EqualTo(2), "and the holder from the old scene is still deaf");
        }
    }
}