using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleFlowTreeBuilderTests
    {
        private readonly FlowConsoleFlowTreeBuilder _builder = new FlowConsoleFlowTreeBuilder();

        private static ConsoleLog Log(string message, int flowId, int parentFlowId = 0)
        {
            return new ConsoleLog {Message = message, FlowId = flowId, ParentFlowId = parentFlowId};
        }

        [Test]
        public void One_dispatch_is_one_root_holding_its_logs_in_order()
        {
            var nodes = _builder.Build(new List<ConsoleLog>
            {
                Log("group", 1), Log("first", 1), Log("second", 1)
            });

            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual(1, nodes[0].FlowId);
            Assert.AreEqual(3, nodes[0].Logs.Count);
            Assert.AreEqual("group", nodes[0].Logs[0].Message);
            Assert.AreEqual("second", nodes[0].Logs[2].Message);
        }

        [Test]
        public void A_nested_dispatch_is_a_child_of_the_flow_that_started_it()
        {
            var nodes = _builder.Build(new List<ConsoleLog>
            {
                Log("outer", 1), Log("inner", 2, 1)
            });

            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual(1, nodes[0].Children.Count);
            Assert.AreEqual(2, nodes[0].Children[0].FlowId);
        }

        [Test]
        public void Two_unrelated_dispatches_are_two_roots()
        {
            var nodes = _builder.Build(new List<ConsoleLog> {Log("a", 1), Log("b", 2)});

            Assert.AreEqual(2, nodes.Count);
        }

        /// <summary>
        /// A log written outside any dispatch - a Unity message, a context phase - still has to
        /// appear, so it is a root holding only itself.
        /// </summary>
        [Test]
        public void A_log_outside_any_flow_is_a_root_of_its_own()
        {
            var nodes = _builder.Build(new List<ConsoleLog> {Log("debug", 0), Log("debug", 0)});

            Assert.AreEqual(2, nodes.Count);
            Assert.AreEqual(1, nodes[0].Logs.Count);
        }

        /// <summary>
        /// MaxLogCount trims the front of the list, so the parent of a flow still on screen may
        /// be gone. The orphan is drawn as a root rather than dropped.
        /// </summary>
        [Test]
        public void A_flow_whose_parent_is_gone_becomes_a_root()
        {
            var nodes = _builder.Build(new List<ConsoleLog> {Log("orphan", 5, 99)});

            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual(5, nodes[0].FlowId);
        }

        [Test]
        public void Roots_keep_the_order_their_flows_first_appeared_in()
        {
            var nodes = _builder.Build(new List<ConsoleLog> {Log("a", 2), Log("b", 1)});

            Assert.AreEqual(2, nodes[0].FlowId);
            Assert.AreEqual(1, nodes[1].FlowId);
        }

        /// <summary>
        /// A command silenced with [HideCommandLog] writes no framework line, so there is no node
        /// for it. A log the developer wrote inside it still carries the flow id and sits under
        /// the flow's root - inventing a node for a command somebody asked to hide would undo the
        /// request.
        /// </summary>
        [Test]
        public void A_silenced_commands_own_line_sits_under_the_flows_root()
        {
            var nodes = _builder.Build(new List<ConsoleLog>
            {
                Log("group", 1), Log("granted 100 soft currency", 1)
            });

            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual(0, nodes[0].Children.Count);
            Assert.AreEqual(2, nodes[0].Logs.Count);
        }

        [Test]
        public void Nothing_builds_an_empty_tree()
        {
            Assert.AreEqual(0, _builder.Build(new List<ConsoleLog>()).Count);
            Assert.AreEqual(0, _builder.Build(null).Count);
        }
    }
}
