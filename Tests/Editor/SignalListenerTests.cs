using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalListenerTests
    {
        [Test]
        public void A_listener_hears_every_dispatch()
        {
            Signal signal = new Signal(true);
            int calls = 0;

            signal.AddListener(() => calls++);

            signal.Dispatch();
            signal.Dispatch();

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void A_removed_listener_hears_nothing()
        {
            Signal signal = new Signal(true);
            int calls = 0;
            void Listener() => calls++;

            signal.AddListener(Listener);
            signal.RemoveListener(Listener);
            signal.Dispatch();

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void A_once_listener_hears_the_first_dispatch_and_no_other()
        {
            Signal signal = new Signal(true);
            int calls = 0;

            signal.AddListenerOnce(() => calls++);

            signal.Dispatch();
            signal.Dispatch();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void A_once_listener_can_be_taken_back_before_it_is_ever_called()
        {
            Signal signal = new Signal(true);
            int calls = 0;
            void Listener() => calls++;

            signal.AddListenerOnce(Listener);
            signal.RemoveListenerOnce(Listener);
            signal.Dispatch();

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Taking_back_a_once_listener_leaves_the_ordinary_one_alone()
        {
            Signal signal = new Signal(true);
            int once = 0;
            int always = 0;
            void OnceListener() => once++;

            signal.AddListenerOnce(OnceListener);
            signal.AddListener(() => always++);

            signal.RemoveListenerOnce(OnceListener);
            signal.Dispatch();

            Assert.That(once, Is.Zero, "the once listener was taken back");
            Assert.That(always, Is.EqualTo(1), "the ordinary listener was not");
        }

        [Test]
        public void A_payload_reaches_the_listener_that_asked_for_it()
        {
            Signal<int> signal = new Signal<int>(true);
            int heard = 0;

            signal.AddListener(value => heard = value);
            signal.Dispatch(42);

            Assert.That(heard, Is.EqualTo(42));
        }

        /// <summary>
        /// A once listener that re-arms itself is the "listen until the answer is right" shape.
        /// Dispatch used to invoke the once list and then null the field, which threw away whatever
        /// the listeners had just added to it.
        /// </summary>
        [Test]
        public void A_once_listener_added_during_a_dispatch_is_heard_on_the_next_one()
        {
            Signal signal = new Signal(true);
            int nested = 0;

            signal.AddListenerOnce(() => signal.AddListenerOnce(() => nested++));

            signal.Dispatch();
            Assert.That(nested, Is.Zero, "the nested listener waits for the next dispatch");

            signal.Dispatch();
            Assert.That(nested, Is.EqualTo(1));
        }

        [Test]
        public void A_once_listener_with_a_payload_added_during_a_dispatch_is_heard_on_the_next_one()
        {
            Signal<int> signal = new Signal<int>(true);
            int heard = 0;

            signal.AddListenerOnce(_ => signal.AddListenerOnce(value => heard = value));

            signal.Dispatch(1);
            signal.Dispatch(2);

            Assert.That(heard, Is.EqualTo(2));
        }

        [Test]
        public void A_once_listener_with_a_payload_can_be_taken_back()
        {
            Signal<int> signal = new Signal<int>(true);
            int calls = 0;
            void Listener(int value) => calls++;

            signal.AddListenerOnce(Listener);
            signal.RemoveListenerOnce(Listener);
            signal.Dispatch(1);

            Assert.That(calls, Is.Zero);
        }

        /// <summary>
        /// A Mediator's OnRemove is the only teardown path a listener has, so a signal that outlives
        /// the objects listening to it keeps every listener a destroyed one left behind. This is the
        /// one call that empties both lists without knowing what is on them.
        /// </summary>
        [Test]
        public void Removing_all_listeners_takes_the_once_listeners_with_them()
        {
            Signal signal = new Signal(true);
            int calls = 0;

            signal.AddListener(() => calls++);
            signal.AddListenerOnce(() => calls++);

            signal.RemoveAllListeners();
            signal.Dispatch();

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Removing_all_listeners_from_a_signal_with_a_payload_empties_both_lists_too()
        {
            Signal<int> signal = new Signal<int>(true);
            int calls = 0;

            signal.AddListener(_ => calls++);
            signal.AddListenerOnce(_ => calls++);

            signal.RemoveAllListeners();
            signal.Dispatch(1);

            Assert.That(calls, Is.Zero);
        }

        /// <summary>
        /// The commands bound to a signal are the context's, not the listener list's, so clearing
        /// listeners must leave them running: a screen dropping its subscriptions cannot be allowed
        /// to silence the flow the rest of the game is dispatching through.
        /// </summary>
        [Test]
        public void Removing_all_listeners_leaves_the_command_callback_alone()
        {
            Signal signal = new Signal(true);
            int commandCalls = 0;

            ((ISignalBody) signal).InternalCallback = (_, _) => commandCalls++;
            signal.AddListener(() => { });

            signal.RemoveAllListeners();
            signal.Dispatch();

            Assert.That(commandCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// Once-listeners, then the commands bound to the signal, then the ordinary listeners. An
        /// ordinary listener therefore reads state a command has already written, which is what a
        /// Mediator relies on when it redraws.
        /// </summary>
        [Test]
        public void Dispatch_runs_once_listeners_then_the_commands_then_the_listeners()
        {
            Signal signal = new Signal(true);
            List<string> order = new();

            signal.AddListener(() => order.Add("listener"));
            signal.AddListenerOnce(() => order.Add("once"));
            ((ISignalBody) signal).InternalCallback = (_, _) => order.Add("command");

            signal.Dispatch();

            Assert.That(order, Is.EqualTo(new[] {"once", "command", "listener"}));
        }
    }
}