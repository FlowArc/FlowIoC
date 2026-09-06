using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Signals;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a Connector actually does when it joins two modules. The connector's bookkeeping is
    /// shared for the whole run, so every test starts and ends by clearing it.
    /// </summary>
    public class SignalConnectorTests
    {
        [SetUp]
        public void SetUp() => SignalConnector.DisconnectAll();

        [TearDown]
        public void TearDown() => SignalConnector.DisconnectAll();

        [Test]
        public void One_signal_dispatched_reaches_the_signal_it_is_connected_to()
        {
            Signal source = new Signal(true);
            Signal target = new Signal(true);
            int heard = 0;

            source.Connect(target);
            target.AddListener(() => heard++);

            source.Dispatch();

            Assert.That(heard, Is.EqualTo(1));
        }

        [Test]
        public void A_payload_crosses_with_the_signal()
        {
            Signal<int> source = new Signal<int>(true);
            Signal<int> target = new Signal<int>(true);
            int heard = 0;

            source.Connect(target);
            target.AddListener(value => heard = value);

            source.Dispatch(5);

            Assert.That(heard, Is.EqualTo(5));
        }

        [Test]
        public void A_converter_adapts_a_payload_the_other_side_does_not_speak()
        {
            Signal<int> source = new Signal<int>(true);
            Signal<string> target = new Signal<string>(true);
            string heard = null;

            source.Connect(target, value => "n=" + value);
            target.AddListener(value => heard = value);

            source.Dispatch(5);

            Assert.That(heard, Is.EqualTo("n=5"));
        }

        [Test]
        public void A_plain_delegate_can_stand_in_for_the_far_signal()
        {
            Signal<int> source = new Signal<int>(true);
            int heard = 0;

            source.Connect(value => heard = value);

            source.Dispatch(5);

            Assert.That(heard, Is.EqualTo(5));
        }

        [Test]
        public void Disconnecting_a_signal_stops_what_it_was_feeding()
        {
            Signal source = new Signal(true);
            Signal target = new Signal(true);
            int heard = 0;

            source.Connect(target);
            target.AddListener(() => heard++);

            source.Disconnect();
            source.Dispatch();

            Assert.That(heard, Is.Zero);
        }

        [Test]
        public void A_group_is_disconnected_as_one()
        {
            Signal first = new Signal(true);
            Signal second = new Signal(true);
            int heard = 0;

            first.Connect(() => heard++, "wiring");
            second.Connect(() => heard++, "wiring");

            SignalConnector.DisconnectGroup("wiring");

            first.Dispatch();
            second.Dispatch();

            Assert.That(heard, Is.Zero);
        }

        [Test]
        public void Disconnecting_one_group_leaves_another_standing()
        {
            Signal inGroup = new Signal(true);
            Signal inOtherGroup = new Signal(true);
            int heard = 0;

            inGroup.Connect(() => heard++, "gone");
            inOtherGroup.Connect(() => heard++, "kept");

            SignalConnector.DisconnectGroup("gone");

            inGroup.Dispatch();
            inOtherGroup.Dispatch();

            Assert.That(heard, Is.EqualTo(1));
        }

        [Test]
        public void DisconnectAll_takes_down_both_the_named_groups_and_the_unnamed_ones()
        {
            Signal named = new Signal(true);
            Signal unnamed = new Signal(true);
            int heard = 0;

            named.Connect(() => heard++, "wiring");
            unnamed.Connect(() => heard++);

            SignalConnector.DisconnectAll();

            named.Dispatch();
            unnamed.Dispatch();

            Assert.That(heard, Is.Zero);
        }

        /// <summary>
        /// Disconnecting a signal only takes down what was wired to it without a group name. A
        /// grouped connection belongs to its group and is taken down with it, which is the point of
        /// naming one.
        /// </summary>
        [Test]
        public void Disconnecting_a_signal_leaves_its_grouped_connections_alone()
        {
            Signal source = new Signal(true);
            int ungrouped = 0;
            int grouped = 0;

            source.Connect(() => ungrouped++);
            source.Connect(() => grouped++, "wiring");

            source.Disconnect();
            source.Dispatch();

            Assert.That(ungrouped, Is.Zero, "the unnamed connection went");
            Assert.That(grouped, Is.EqualTo(1), "the named one is its group's to take down");
        }
    }
}
