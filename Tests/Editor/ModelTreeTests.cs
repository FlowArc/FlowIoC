using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.BaseModule.Signals;
using FlowIoC.Editor.ModelViewer;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the Model Viewer lists under a Root: the objects the module bound, in its own binder
    /// and across, less the framework's own plumbing and the module's signal holders, badged by
    /// what their name says they are.
    /// </summary>
    public class ModelTreeTests
    {
        private class ProbeModel
        {
        }

        private interface IProbeModel
        {
        }

        private class KeyedModel : IProbeModel
        {
        }

        private class ProbeService
        {
        }

        private class ProbeSystem
        {
        }

        private class ProbeSubService
        {
        }

        private class ProbeSubSystem
        {
        }

        private class ProbeThing
        {
        }

        [HideInModelViewer]
        private class HiddenThing
        {
        }

        private class ProbeSignals : ISignalHolder
        {
        }

        private InjectionBinderCrossContext _crossContext;
        private readonly List<GameObject> _hosts = new List<GameObject>();
        private readonly List<Context> _contexts = new List<Context>();
        private readonly ModelTree _tree = new ModelTree();

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

            _crossContext.UnBind<ProbeSystem>();
            _crossContext.UnBind<ProbeSignals>();
            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();

            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        /// <summary>A context the way a Root builds one: on a host object, started, with its own binder.</summary>
        private Context Start(string name, List<IContext> subContexts = null)
        {
            var host = new GameObject(name);
            _hosts.Add(host);

            var context = new Context();
            context.Initialize(host, 0, _crossContext, subContexts ?? new List<IContext>());
            context.Start();
            _contexts.Add(context);

            return context;
        }

        private RootBase Root(string name, int order, IContext context)
        {
            var host = new GameObject(name);
            _hosts.Add(host);

            RootBase root = host.AddComponent<RootBase>();
            root.initializeOrder = order;
            root.Context = context;

            return root;
        }

        private static IEnumerable<string> TypeNames(IEnumerable<ModelObjectEVO> objects) =>
            objects.Select(entry => entry.Value.GetType().Name);

        // ---- Objects of a context

        [Test]
        public void A_started_context_with_one_model_lists_the_model_and_none_of_the_plumbing()
        {
            Context context = Start("Host");
            context.InjectionBinder.Bind<ProbeModel>();

            CollectionAssert.AreEqual(new[] {"ProbeModel"}, TypeNames(_tree.Objects(context)));
        }

        [Test]
        public void A_signal_holder_a_hidden_type_and_a_unity_object_are_not_listed()
        {
            Context context = Start("Host");
            context.InjectionBinder.Bind<ProbeSignals>();
            context.InjectionBinder.BindInstance(new HiddenThing());
            context.InjectionBinder.BindInstance<GameObject>(_hosts[0], "probe");
            context.InjectionBinder.Bind<ProbeModel>();

            CollectionAssert.AreEqual(new[] {"ProbeModel"}, TypeNames(_tree.Objects(context)));
        }

        [Test]
        public void What_a_context_bound_across_is_listed_under_it_and_not_under_another()
        {
            Context first = Start("First");
            _crossContext.Bind<ProbeSystem>();
            Context second = Start("Second");

            CollectionAssert.Contains(TypeNames(_tree.Objects(first)).ToList(), "ProbeSystem");
            CollectionAssert.DoesNotContain(TypeNames(_tree.Objects(second)).ToList(), "ProbeSystem");
        }

        /// <summary>
        /// The second binding carries a name: without one the binder finds the first through its
        /// assignable-type scan and refuses the second as a duplicate, which is the binder's rule
        /// and not this one's.
        /// </summary>
        [Test]
        public void One_instance_bound_under_two_keys_is_listed_once()
        {
            Context context = Start("Host");
            var model = new KeyedModel();
            context.InjectionBinder.BindInstance(model);
            context.InjectionBinder.BindInstance<IProbeModel>(model, "second");

            List<ModelObjectEVO> objects = _tree.Objects(context);

            Assert.That(objects.Count, Is.EqualTo(1));
            Assert.That(objects[0].Key, Is.EqualTo(typeof(KeyedModel)), "the first key is the one kept");
        }

        [Test]
        public void Objects_are_ordered_by_kind_then_by_name()
        {
            Context context = Start("Host");
            context.InjectionBinder.Bind<ProbeThing>();
            context.InjectionBinder.Bind<ProbeSubSystem>();
            context.InjectionBinder.Bind<ProbeSubService>();
            context.InjectionBinder.Bind<ProbeService>();
            context.InjectionBinder.Bind<ProbeModel>();
            _crossContext.Bind<ProbeSystem>();

            CollectionAssert.AreEqual(
                new[] {"ProbeModel", "ProbeService", "ProbeSystem", "ProbeSubService", "ProbeSubSystem", "ProbeThing"},
                TypeNames(_tree.Objects(context)));
        }

        [Test]
        public void A_named_binding_keeps_its_name_and_key()
        {
            Context context = Start("Host");
            context.InjectionBinder.Bind<IProbeModel, KeyedModel>("second");

            ModelObjectEVO entry = _tree.Objects(context).Single();

            Assert.That(entry.Name, Is.EqualTo("second"));
            Assert.That(entry.Key, Is.EqualTo(typeof(IProbeModel)));
            Assert.That(entry.Kind, Is.EqualTo(ModelKind.Model));
        }

        // ---- Kind

        [Test]
        public void Kind_is_read_off_the_type_name_longest_suffix_first()
        {
            Assert.That(_tree.KindOf(typeof(ProbeModel)), Is.EqualTo(ModelKind.Model));
            Assert.That(_tree.KindOf(typeof(ProbeService)), Is.EqualTo(ModelKind.Service));
            Assert.That(_tree.KindOf(typeof(ProbeSystem)), Is.EqualTo(ModelKind.System));
            Assert.That(_tree.KindOf(typeof(ProbeSubService)), Is.EqualTo(ModelKind.SubService));
            Assert.That(_tree.KindOf(typeof(ProbeSubSystem)), Is.EqualTo(ModelKind.SubSystem));
            Assert.That(_tree.KindOf(typeof(ProbeThing)), Is.EqualTo(ModelKind.Other));
        }

        // ---- Shared

        [Test]
        public void The_shared_row_holds_what_was_bound_before_any_context_and_none_of_the_plumbing()
        {
            Start("Host");

            List<ModelObjectEVO> shared = _tree.Shared(_crossContext);

            Assert.That(shared.Any(entry => entry.Value is SharedDataModel), Is.True);
            Assert.That(shared.All(entry => !(entry.Value is Object)), Is.True, "no GameObject, no provider");
            Assert.That(shared.All(entry => !(entry.Value is InjectionBinderCrossContext)), Is.True, "hidden by attribute");
        }

        // ---- Build

        [Test]
        public void Roots_come_in_initialize_order_and_a_root_without_a_context_is_left_out()
        {
            RootBase late = Root("Late", 5, Start("LateHost"));
            RootBase early = Root("Early", -3, Start("EarlyHost"));
            RootBase unstarted = Root("Unstarted", 0, null);

            List<ModelRootEVO> rows = _tree.Build(new[] {late, unstarted, early});

            CollectionAssert.AreEqual(new[] {"Early", "Late"}, rows.Select(row => row.Root.name));
        }

        [Test]
        public void A_sub_context_gets_a_row_only_when_it_lists_something()
        {
            Context filled = Start("FilledSub");
            filled.InjectionBinder.Bind<ProbeModel>();
            Context empty = Start("EmptySub");
            Context main = Start("MainHost", new List<IContext> {filled, empty});
            main.InjectionBinder.Bind<ProbeService>();
            RootBase root = Root("Root", 0, main);

            ModelRootEVO row = _tree.Build(new[] {root}).Single();

            CollectionAssert.AreEqual(new[] {"ProbeService"}, TypeNames(row.Objects));
            Assert.That(row.SubContexts.Count, Is.EqualTo(1));
            Assert.That(row.SubContexts[0].Context, Is.SameAs(filled));
            CollectionAssert.AreEqual(new[] {"ProbeModel"}, TypeNames(row.SubContexts[0].Objects));
        }

        [Test]
        public void A_plain_root_wears_the_root_role()
        {
            RootBase root = Root("Root", 0, Start("Host"));

            Assert.That(_tree.Build(new[] {root}).Single().Role, Is.EqualTo(FlowRole.Root));
        }
    }
}
