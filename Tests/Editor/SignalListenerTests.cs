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
    }
}
