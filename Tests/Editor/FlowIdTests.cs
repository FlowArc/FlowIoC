using System.Diagnostics;
using System.Reflection;
using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowIdTests
    {
        [TearDown]
        public void LeaveNoFlowCurrent() => FlowLogger.ExitFlow(0, 0);

        [Test]
        public void Two_flows_do_not_share_an_id()
        {
            int first = 0, firstParent = 0;
            int second = 0, secondParent = 0;

            FlowLogger.NextFlowId(ref first, ref firstParent);
            FlowLogger.NextFlowId(ref second, ref secondParent);

            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(0, first);
        }

        [Test]
        public void A_flow_started_outside_any_other_has_no_parent()
        {
            FlowLogger.ExitFlow(0, 0);

            int id = 0, parent = 0;
            FlowLogger.NextFlowId(ref id, ref parent);

            Assert.AreEqual(0, parent);
        }

        /// <summary>
        /// A signal dispatched from inside a command belongs under it in the tree, which is
        /// what the parent id is for.
        /// </summary>
        [Test]
        public void A_flow_started_inside_another_records_it_as_its_parent()
        {
            int outer = 0, outerParent = 0;
            FlowLogger.NextFlowId(ref outer, ref outerParent);

            int previousFlow = 0, previousParent = 0;
            FlowLogger.EnterFlow(outer, outerParent, ref previousFlow, ref previousParent);

            int inner = 0, innerParent = 0;
            FlowLogger.NextFlowId(ref inner, ref innerParent);

            Assert.AreEqual(outer, innerParent);

            FlowLogger.ExitFlow(previousFlow, previousParent);
        }

        [Test]
        public void Leaving_a_flow_puts_the_one_around_it_back()
        {
            int outer = 0, outerParent = 0;
            FlowLogger.NextFlowId(ref outer, ref outerParent);

            int previousOuterFlow = 0, previousOuterParent = 0;
            FlowLogger.EnterFlow(outer, outerParent, ref previousOuterFlow, ref previousOuterParent);

            int inner = 0, innerParent = 0;
            FlowLogger.NextFlowId(ref inner, ref innerParent);

            int previousInnerFlow = 0, previousInnerParent = 0;
            FlowLogger.EnterFlow(inner, innerParent, ref previousInnerFlow, ref previousInnerParent);
            Assert.AreEqual(inner, FlowLogger.CurrentFlowId);
            Assert.AreEqual(outer, FlowLogger.CurrentParentFlowId);

            FlowLogger.ExitFlow(previousInnerFlow, previousInnerParent);
            Assert.AreEqual(outer, FlowLogger.CurrentFlowId);

            FlowLogger.ExitFlow(previousOuterFlow, previousOuterParent);
        }

        /// <summary>
        /// A log written three steps into a chain has to name the same parent as the one
        /// written at its start, so the parent is carried with the flow rather than worked
        /// out at write time.
        /// </summary>
        [Test]
        public void The_parent_stays_put_for_as_long_as_the_flow_does()
        {
            int outer = 0, outerParent = 0;
            FlowLogger.NextFlowId(ref outer, ref outerParent);

            int previousFlow = 0, previousParent = 0;
            FlowLogger.EnterFlow(outer, outerParent, ref previousFlow, ref previousParent);

            int inner = 0, innerParent = 0;
            FlowLogger.NextFlowId(ref inner, ref innerParent);

            int innerPreviousFlow = 0, innerPreviousParent = 0;
            FlowLogger.EnterFlow(inner, innerParent, ref innerPreviousFlow, ref innerPreviousParent);

            int unrelated = 0, unrelatedParent = 0;
            FlowLogger.NextFlowId(ref unrelated, ref unrelatedParent);

            Assert.AreEqual(outer, FlowLogger.CurrentParentFlowId, "taking an id must not move the parent");

            FlowLogger.ExitFlow(innerPreviousFlow, innerPreviousParent);
            FlowLogger.ExitFlow(previousFlow, previousParent);
        }

        /// <summary>
        /// The guard on the claim that a shipping build pays nothing for the flow tree. Without
        /// [Conditional] these calls would survive into a player build.
        /// </summary>
        [Test]
        public void Every_flow_mutator_compiles_out_without_ENABLE_LOG()
        {
            AssertConditional(nameof(FlowLogger.NextFlowId));
            AssertConditional(nameof(FlowLogger.EnterFlow));
            AssertConditional(nameof(FlowLogger.ExitFlow));
            AssertConditional(nameof(FlowLogger.CaptureCurrentFlow));
        }

        private static void AssertConditional(string methodName)
        {
            MethodInfo method = typeof(FlowLogger).GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, methodName + " is missing.");

            var attributes = (ConditionalAttribute[])
                method.GetCustomAttributes(typeof(ConditionalAttribute), false);

            Assert.AreEqual(1, attributes.Length, methodName + " carries no [Conditional].");
            Assert.AreEqual("ENABLE_LOG", attributes[0].ConditionString);
        }
    }
}