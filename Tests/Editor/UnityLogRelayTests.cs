using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    public class UnityLogRelayTests
    {
        private class Received
        {
            public string Message;
            public LogType Type;
            public bool IsEcho;
            public int ThreadId;
        }

        /// <summary>What Unity's context does: keeps the callback and runs it on the next frame.</summary>
        private class RecordingContext : SynchronizationContext
        {
            public readonly List<SendOrPostCallback> Posted = new();

            public override void Post(SendOrPostCallback d, object state) => Posted.Add(d);

            public void RunPosted()
            {
                foreach (SendOrPostCallback callback in Posted) callback(null);
                Posted.Clear();
            }
        }

        private readonly List<Received> _received = new();
        private bool _echoNow;

        [SetUp]
        public void Clear()
        {
            _received.Clear();
            _echoNow = false;
        }

        private UnityLogRelay Relay(SynchronizationContext context = null)
        {
            return new UnityLogRelay(
                (message, trace, type, isEcho) => _received.Add(new Received
                {
                    Message = message, Type = type, IsEcho = isEcho, ThreadId = Thread.CurrentThread.ManagedThreadId
                }),
                () => _echoNow,
                context);
        }

        [Test]
        public void A_line_from_the_main_thread_is_handed_over_at_once_on_that_thread()
        {
            UnityLogRelay relay = Relay();

            relay.Receive("main", null, LogType.Warning);

            Assert.AreEqual(1, _received.Count);
            Assert.AreEqual("main", _received[0].Message);
            Assert.AreEqual(LogType.Warning, _received[0].Type);
            Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, _received[0].ThreadId);
        }

        [Test]
        public void A_line_from_another_thread_waits_until_the_main_thread_drains_it()
        {
            UnityLogRelay relay = Relay();

            Task.Run(() => relay.Receive("worker", null, LogType.Log)).Wait();

            Assert.IsEmpty(_received, "nothing is handed over on the thread that wrote the line");

            relay.Drain();

            Assert.AreEqual(1, _received.Count);
            Assert.AreEqual("worker", _received[0].Message);
            Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, _received[0].ThreadId);
        }

        [Test]
        public void Lines_from_another_thread_are_drained_in_the_order_they_were_written()
        {
            UnityLogRelay relay = Relay();

            Task.Run(() =>
            {
                relay.Receive("first", null, LogType.Log);
                relay.Receive("second", null, LogType.Log);
            }).Wait();

            relay.Drain();

            Assert.AreEqual(2, _received.Count);
            Assert.AreEqual("first", _received[0].Message);
            Assert.AreEqual("second", _received[1].Message);
        }

        [Test]
        public void With_a_context_a_line_from_another_thread_is_posted_to_it()
        {
            var context = new RecordingContext();
            UnityLogRelay relay = Relay(context);

            Task.Run(() => relay.Receive("worker", null, LogType.Error)).Wait();

            Assert.AreEqual(1, context.Posted.Count);
            Assert.IsEmpty(_received);

            context.RunPosted();

            Assert.AreEqual(1, _received.Count);
            Assert.AreEqual("worker", _received[0].Message);
        }

        /// <summary>
        /// FlowLogger raises its flag only around its own write, so whether a line is that write
        /// coming back has to be read where the line was written, not where it is handed over.
        /// </summary>
        [Test]
        public void Whether_a_line_is_an_echo_is_read_on_the_thread_that_wrote_it()
        {
            UnityLogRelay relay = Relay();

            Task.Run(() =>
            {
                _echoNow = true;
                relay.Receive("ours", null, LogType.Log);
                _echoNow = false;
                relay.Receive("theirs", null, LogType.Log);
            }).Wait();

            relay.Drain();

            Assert.IsTrue(_received[0].IsEcho);
            Assert.IsFalse(_received[1].IsEcho);
        }

        [Test]
        public void Draining_with_nothing_waiting_hands_over_nothing()
        {
            Relay().Drain();

            Assert.IsEmpty(_received);
        }

        /// <summary>
        /// The whole road in the editor: a Debug.Log from a Task, which Application.logMessageReceived
        /// never delivers, reaches Flow Console through the bridge within a few frames.
        /// </summary>
        [UnityTest]
        public IEnumerator A_Debug_Log_from_a_Task_reaches_the_console()
        {
            const string text = "UnityLogRelayTests - a line from a Task";
            FlowLogger.ClearLogs();

            Task.Run(() => Debug.Log(text)).Wait();

            double deadline = EditorApplication.timeSinceStartup + 5;
            while (EditorApplication.timeSinceStartup < deadline && RowsMentioning(text) == 0)
                yield return null;

            Assert.AreEqual(1, RowsMentioning(text));
            FlowLogger.ClearLogs();
        }

        private static int RowsMentioning(string text)
        {
            int rows = 0;

            foreach (ConsoleLog log in FlowLogger.Logs)
            {
                if (log.Message != null && log.Message.Contains(text)) rows++;
            }

            return rows;
        }
    }
}
