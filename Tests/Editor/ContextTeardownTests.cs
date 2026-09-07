using System.Collections.Generic;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a context takes back from the shared binder on its way out. The shared binder lives for
    /// the run, so a module's holder and Service used to outlive the module: when its Root was
    /// built again - a scene coming back - the old instances came back with it, still pointing at
    /// models the old context had already torn down.
    /// </summary>
    public class ContextTeardownTests
    {
        private class Holder
        {
        }

        private class OtherHolder
        {
        }

        private class HandedIn
        {
        }

        private class Constructable : IConstructable
        {
            public bool IsPostConstructed { get; set; }
            public bool IsDeConstructed { get; set; }
            public bool Deconstructed;

            public void PostConstruct()
            {
            }

            public void Deconstruct() => Deconstructed = true;
        }

        private InjectionBinderCrossContext _crossContext;
        private readonly List<GameObject> _hosts = new();
        private readonly List<Context> _contexts = new();

        [SetUp]
        public void SetUp()
        {
            _crossContext = ((RootsManager) RootsManagerFactory.GetRootsManager()).InjectionBinderCrossContext;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Context context in _contexts)
            {
                if (context.IsStarted)
                    context.DestroyContext();
            }

            _contexts.Clear();

            foreach (GameObject host in _hosts)
                Object.DestroyImmediate(host);

            _hosts.Clear();

            // The shared binder is the run's, so whatever a failed assertion left behind is taken
            // out here rather than reaching the next test.
            _crossContext.UnBind<Holder>();
            _crossContext.UnBind<OtherHolder>();
            _crossContext.UnBind<HandedIn>();
            _crossContext.UnBind<Constructable>();
            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();

            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        /// <summary>
        /// A context the way a Root builds one: initialised on a host object, started, and so the
        /// one the shared binder currently binds on behalf of.
        /// </summary>
        private Context Start(string name)
        {
            GameObject host = new GameObject(name);
            _hosts.Add(host);

            Context context = new Context();
            context.Initialize(host, 0, _crossContext, new List<IContext>());
            context.Start();
            _contexts.Add(context);

            return context;
        }

        [Test]
        public void A_context_takes_back_what_it_bound_across()
        {
            Context context = Start("PlayerContextHost");
            _crossContext.Bind<Holder>();

            Assert.That(_crossContext.HasBinding<Holder>(), Is.True);

            context.DestroyContext();

            Assert.That(_crossContext.HasBinding<Holder>(), Is.False);
        }

        [Test]
        public void What_another_context_bound_across_stays()
        {
            Context first = Start("FirstHost");
            _crossContext.Bind<Holder>();

            Start("SecondHost");
            _crossContext.Bind<OtherHolder>();

            first.DestroyContext();

            Assert.That(_crossContext.HasBinding<Holder>(), Is.False, "the first context's binding went with it");
            Assert.That(_crossContext.HasBinding<OtherHolder>(), Is.True, "the second context's did not");
        }

        /// <summary>
        /// The scene coming back. The rebuilt context binds a holder of its own rather than being
        /// handed the instance the torn-down context left behind.
        /// </summary>
        [Test]
        public void A_context_built_again_binds_a_fresh_instance()
        {
            Context first = Start("PlayerContextHost");
            Holder before = _crossContext.Bind<Holder>();
            first.DestroyContext();

            Start("PlayerContextHost");
            Holder after = _crossContext.Bind<Holder>();

            Assert.That(after, Is.Not.SameAs(before));
        }

        [Test]
        public void Deconstruct_runs_for_what_was_bound_across()
        {
            Context context = Start("PlayerContextHost");
            Constructable constructable = _crossContext.Bind<Constructable>();
            constructable.IsPostConstructed = true;

            context.DestroyContext();

            Assert.That(constructable.Deconstructed, Is.True);
        }

        /// <summary>
        /// BindInstance sets no context - the providers are bound that way, for the whole run - so
        /// nothing a context tears down reaches it.
        /// </summary>
        [Test]
        public void What_was_handed_in_with_BindInstance_stays()
        {
            Context context = Start("PlayerContextHost");
            _crossContext.BindInstance<HandedIn>(new HandedIn());

            context.DestroyContext();

            Assert.That(_crossContext.HasBinding<HandedIn>(), Is.True);
        }
    }
}
