using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Asking the binder for something it does not have. A Connector gets signal holders rather
    /// than binding them, so this is the path a missing module takes, and it has to say which
    /// type is missing instead of throwing a dictionary's key error at whoever asked.
    /// </summary>
    public class InjectionBinderTests
    {
        private class Holder
        {
        }

        private InjectionBinder _binder;

        [SetUp]
        public void SetUp()
        {
            _binder = new InjectionBinder();
            _binder.SetBoundContext(new Context());
        }

        [Test]
        public void Asking_for_a_type_nobody_bound_names_the_type()
        {
            LogAssert.Expect(LogType.Error, new Regex("Nothing is bound to Holder"));

            Assert.IsNull(_binder.GetInstance<Holder>());
        }

        [Test]
        public void Asking_under_a_name_nobody_bound_says_the_type_is_there_and_the_name_is_not()
        {
            _binder.Bind<Holder>();

            LogAssert.Expect(LogType.Error, new Regex("Holder is bound, but not under the name 'second'"));

            Assert.IsNull(_binder.GetInstance<Holder>("second"));
        }

        [Test]
        public void What_was_bound_comes_back()
        {
            Holder bound = _binder.Bind<Holder>();

            Assert.AreSame(bound, _binder.GetInstance<Holder>());
        }

        /// <summary>
        /// Unbinding by instance used to check the wrong variable for null and then read the
        /// binding it had not found, so an instance that was never bound threw instead of being
        /// left alone.
        /// </summary>
        [Test]
        public void Unbinding_an_instance_that_was_never_bound_changes_nothing()
        {
            Holder bound = _binder.Bind<Holder>();

            _binder.UnBind<Holder>(new Holder());

            Assert.AreSame(bound, _binder.GetInstance<Holder>());
        }

        [Test]
        public void Unbinding_an_instance_of_a_type_nobody_bound_changes_nothing()
        {
            _binder.UnBind<Holder>(new Holder());

            LogAssert.Expect(LogType.Error, new Regex("Nothing is bound to Holder"));

            Assert.IsNull(_binder.GetInstance<Holder>());
        }

        [Test]
        public void Unbinding_by_instance_takes_that_binding_out()
        {
            Holder bound = _binder.Bind<Holder>();

            _binder.UnBind<Holder>(bound);

            LogAssert.Expect(LogType.Error, new Regex("Nothing is bound to Holder"));

            Assert.IsNull(_binder.GetInstance<Holder>());
        }

        #region The assignable-type cache

        private interface IThing
        {
        }

        private class Thing : IThing
        {
        }

        /// <summary>
        /// A type nobody bound is answered for by whatever is bound under something assignable to
        /// it, which is how <c>Bind&lt;IPlayerModel, PlayerModel&gt;</c> and a bare
        /// <c>Bind&lt;PlayerModel&gt;</c> both answer an <c>[Inject] IPlayerModel</c>.
        /// </summary>
        [Test]
        public void A_type_nobody_bound_is_answered_by_something_assignable_to_it()
        {
            Thing bound = _binder.Bind<Thing>();

            Assert.AreSame(bound, _binder.GetInstance(typeof(IThing)));
            Assert.AreSame(bound, _binder.GetInstance(typeof(IThing)), "and the same way when it is asked again");
        }

        /// <summary>
        /// The scan is expensive and every injection asks every context in turn, so most calls land
        /// on a binder that has nothing and the answer is remembered - misses included. A remembered
        /// miss that outlived the bind that answered it is the bug this guards: a module bound after
        /// something else asked for it would never be found.
        /// </summary>
        [Test]
        public void A_remembered_miss_does_not_survive_the_binding_that_answers_it()
        {
            Assert.IsNull(_binder.GetInstance(typeof(IThing)), "nothing is bound yet");

            Thing bound = _binder.Bind<Thing>();

            Assert.AreSame(bound, _binder.GetInstance(typeof(IThing)));
        }

        [Test]
        public void A_remembered_answer_does_not_survive_the_binding_going_away()
        {
            Thing bound = _binder.Bind<Thing>();
            Assert.AreSame(bound, _binder.GetInstance(typeof(IThing)));

            _binder.UnBind<Thing>(bound);

            Assert.IsNull(_binder.GetInstance(typeof(IThing)));
        }

        #endregion

        #region The binding generation

        /// <summary>
        /// What a pooled command resolved is remembered against this number, so it has to move
        /// whenever the container's shape changes - and stay put when nothing did.
        /// </summary>
        [Test]
        public void Binding_something_moves_the_generation()
        {
            int before = _binder.BindingGeneration;

            _binder.Bind<Holder>();

            Assert.That(_binder.BindingGeneration, Is.GreaterThan(before));
        }

        [Test]
        public void Unbinding_something_moves_the_generation()
        {
            Holder bound = _binder.Bind<Holder>();
            int before = _binder.BindingGeneration;

            _binder.UnBind<Holder>(bound);

            Assert.That(_binder.BindingGeneration, Is.GreaterThan(before));
        }

        /// <summary>
        /// The number counts the whole run rather than one binder, because a command resolves
        /// against every context in turn: a binding gained in another module's context is exactly
        /// as able to change what this command should be holding.
        /// </summary>
        [Test]
        public void The_generation_counts_every_binder_in_the_run()
        {
            InjectionBinder other = new InjectionBinder();
            other.SetBoundContext(new Context());

            int before = _binder.BindingGeneration;

            other.Bind<Holder>();

            Assert.That(_binder.BindingGeneration, Is.GreaterThan(before));
        }

        /// <summary>
        /// A second BindInstance under a type that already has one is refused, and a refusal has
        /// changed nothing - so it must not send every pooled command in the run to resolve its
        /// members again.
        /// </summary>
        [Test]
        public void A_bind_that_changed_nothing_leaves_the_generation_where_it_was()
        {
            _binder.BindInstance<Holder>(new Holder());
            int before = _binder.BindingGeneration;

            _binder.BindInstance<Holder>(new Holder());

            Assert.That(_binder.BindingGeneration, Is.EqualTo(before));
        }

        #endregion
    }
}