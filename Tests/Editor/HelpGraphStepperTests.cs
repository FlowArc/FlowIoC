using FlowIoC.Editor.Help.Graph;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class HelpGraphStepperTests
    {
        [Test]
        public void A_walk_starts_on_the_first_step()
        {
            HelpGraphStepper stepper = new HelpGraphStepper(4);

            Assert.AreEqual(0, stepper.Index);
            Assert.AreEqual(4, stepper.Count);
            Assert.IsFalse(stepper.CanGoPrevious);
            Assert.IsTrue(stepper.CanGoNext);
        }

        [Test]
        public void Next_stops_on_the_last_step()
        {
            HelpGraphStepper stepper = new HelpGraphStepper(3);

            stepper.Next();
            stepper.Next();
            stepper.Next();
            stepper.Next();

            Assert.AreEqual(2, stepper.Index);
            Assert.IsFalse(stepper.CanGoNext);
        }

        [Test]
        public void Previous_stops_on_the_first_step()
        {
            HelpGraphStepper stepper = new HelpGraphStepper(3);

            stepper.Next();
            stepper.Previous();
            stepper.Previous();

            Assert.AreEqual(0, stepper.Index);
        }

        [Test]
        public void Reset_returns_to_the_beginning()
        {
            HelpGraphStepper stepper = new HelpGraphStepper(3);

            stepper.Next();
            stepper.Reset();

            Assert.AreEqual(0, stepper.Index);
        }

        /// <summary>
        /// A page without a diagram still asks the window to draw it, so a count of zero has to be
        /// a quiet no-op rather than an index out of range.
        /// </summary>
        [Test]
        public void A_walk_with_no_steps_goes_nowhere()
        {
            HelpGraphStepper stepper = new HelpGraphStepper(0);

            stepper.Next();

            Assert.AreEqual(0, stepper.Index);
            Assert.IsFalse(stepper.CanGoNext);
            Assert.IsFalse(stepper.CanGoPrevious);
        }
    }
}
