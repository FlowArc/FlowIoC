using System.Text;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class PlayerLogEnvelopeTests
    {
        [Test]
        public void A_log_comes_back_whole()
        {
            var log = new ConsoleLog
            {
                Channel = "Signal", LogType = LogType.Warning, Message = "hello", StackTrace = "A:B() (at Assets/A.cs:3)",
                FlowId = 7, ParentFlowId = 2, Frame = 40, Realtime = 1.5f, Hour = 13, Minute = 5, Second = 23,
                Millisecond = 88, Source = LogSource.Flow, SystemLogType = SystemLogType.Signal, InPlayMode = true
            };

            PlayerLogEnvelope read = PlayerLogEnvelope.Read(PlayerLogEnvelope.ForLog(log).Write());

            Assert.AreEqual(PlayerLogKind.Log, read.Kind);
            Assert.AreEqual("Signal", read.Log.Channel);
            Assert.AreEqual(LogType.Warning, read.Log.LogType);
            Assert.AreEqual("hello", read.Log.Message);
            Assert.AreEqual("A:B() (at Assets/A.cs:3)", read.Log.StackTrace);
            Assert.AreEqual(7, read.Log.FlowId);
            Assert.AreEqual(2, read.Log.ParentFlowId);
            Assert.AreEqual(40, read.Log.Frame);
            Assert.AreEqual(1.5f, read.Log.Realtime);
            Assert.AreEqual(88, read.Log.Millisecond);
            Assert.AreEqual(LogSource.Flow, read.Log.Source);
            Assert.AreEqual(SystemLogType.Signal, read.Log.SystemLogType);
            Assert.IsTrue(read.Log.InPlayMode);
        }

        [Test]
        public void A_hello_carries_the_players_name_and_no_log()
        {
            PlayerLogEnvelope read = PlayerLogEnvelope.Read(PlayerLogEnvelope.Hello("Android Pixel 7").Write());

            Assert.AreEqual(PlayerLogKind.Hello, read.Kind);
            Assert.AreEqual("Android Pixel 7", read.Player);
        }

        /// <summary>
        /// The two ends can be on different package versions. A field one side does not know is
        /// skipped and one it did not receive keeps its default, rather than either end failing.
        /// </summary>
        [Test]
        public void A_missing_field_reads_as_its_default()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("{\"Kind\":1,\"Log\":{\"Message\":\"only this\"}}");

            PlayerLogEnvelope read = PlayerLogEnvelope.Read(bytes);

            Assert.AreEqual(PlayerLogKind.Log, read.Kind);
            Assert.AreEqual("only this", read.Log.Message);
            Assert.AreEqual(0, read.Log.FlowId);
            Assert.IsTrue(string.IsNullOrEmpty(read.Player));
        }

        [Test]
        public void Bytes_that_are_not_an_envelope_read_as_nothing()
        {
            Assert.IsNull(PlayerLogEnvelope.Read(Encoding.UTF8.GetBytes("not json")));
            Assert.IsNull(PlayerLogEnvelope.Read(null));
            Assert.IsNull(PlayerLogEnvelope.Read(new byte[0]));
        }
    }
}
