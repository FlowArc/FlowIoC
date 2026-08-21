using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalParamInjectionTests
    {
        private class MoveCommand : Command
        {
            [SignalParam(0)] private int _x { get; set; }
            [SignalParam(1)] private int _y { get; set; }

            public int X => _x;
            public int Y => _y;

            public override void Execute() { }
        }

        private class DamageCommand : Command
        {
            [SignalParam] private string _weapon { get; set; }
            [SignalParam(0)] private int _amount { get; set; }
            [SignalParam(1)] private int _crit { get; set; }

            public string Weapon => _weapon;
            public int Amount => _amount;
            public int Crit => _crit;

            public override void Execute() { }
        }

        private class ReentrantCommand : Command
        {
            private int _x;
            private int _y;

            public IContext OwningContext { get; set; }
            public MoveCommand Nested { get; private set; }

            [SignalParam]
            private int X
            {
                get => _x;
                set
                {
                    _x = value;
                    if (Nested != null) return;

                    // A setter that dispatches re-enters injection on the same context,
                    // which shares one resolver instance with the call still in progress.
                    Nested = new MoveCommand();
                    InjectionExtensions.InjectCommand(OwningContext, Nested, 100, 200);
                }
            }

            [SignalParam]
            private int Y
            {
                get => _y;
                set => _y = value;
            }

            public int XValue => _x;
            public int YValue => _y;

            public override void Execute() { }
        }

        [Test]
        public void InjectCommand_fills_same_typed_properties_from_distinct_slots()
        {
            var command = new MoveCommand();

            InjectionExtensions.InjectCommand(null, command, 3, 7);

            Assert.That(command.X, Is.EqualTo(3));
            Assert.That(command.Y, Is.EqualTo(7));
        }

        [Test]
        public void InjectCommand_mixes_indexed_and_unindexed_properties()
        {
            var command = new DamageCommand();

            InjectionExtensions.InjectCommand(null, command, "sword", 12, 3);

            Assert.That(command.Weapon, Is.EqualTo("sword"));
            Assert.That(command.Amount, Is.EqualTo(12));
            Assert.That(command.Crit, Is.EqualTo(3));
        }

        [Test]
        public void InjectCommand_binds_two_instances_of_the_same_command_type_independently()
        {
            var first = new MoveCommand();
            var second = new MoveCommand();

            InjectionExtensions.InjectCommand(null, first, 1, 2);
            InjectionExtensions.InjectCommand(null, second, 8, 9);

            Assert.That(first.X, Is.EqualTo(1));
            Assert.That(first.Y, Is.EqualTo(2));
            Assert.That(second.X, Is.EqualTo(8));
            Assert.That(second.Y, Is.EqualTo(9));
        }

        [Test]
        public void A_nested_injection_on_the_same_context_does_not_corrupt_the_outer_one()
        {
            var context = new Context();
            var outer = new ReentrantCommand { OwningContext = context };

            InjectionExtensions.InjectCommand(context, outer, 3, 7);

            Assert.That(outer.Nested, Is.Not.Null);
            Assert.That(outer.Nested.X, Is.EqualTo(100));
            Assert.That(outer.Nested.Y, Is.EqualTo(200));

            // Without the re-entrancy guard the nested call clears the claim on slot 0,
            // so the outer _y takes it back and binds 3 instead of 7.
            Assert.That(outer.XValue, Is.EqualTo(3));
            Assert.That(outer.YValue, Is.EqualTo(7));
        }
    }
}
