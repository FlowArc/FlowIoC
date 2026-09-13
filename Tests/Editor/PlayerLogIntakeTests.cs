using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class PlayerLogIntakeTests
    {
        private PlayerLogIntake _intake;

        /// <summary>A fresh one per test: the intake remembers players, and NUnit reuses the fixture.</summary>
        [SetUp]
        public void SetUp()
        {
            _intake = new PlayerLogIntake();
        }

        private static byte[] Hello(string name) => PlayerLogEnvelope.Hello(name).Write();

        private static byte[] Row(int flowId, int parentFlowId)
        {
            return PlayerLogEnvelope.ForLog(new ConsoleLog
            {
                Channel = "Signal", LogType = LogType.Log, Message = "row", FlowId = flowId, ParentFlowId = parentFlowId
            }).Write();
        }

        [Test]
        public void A_hello_records_nothing_and_says_a_player_is_present()
        {
            Assert.IsFalse(_intake.IsFlowPlayerPresent);

            Assert.IsNull(_intake.Receive(3, Hello("Android Pixel 7"), "AndroidPlayer(Pixel 7)"));

            Assert.IsTrue(_intake.IsFlowPlayerPresent);
            Assert.AreEqual("Android Pixel 7", _intake.NameOf(3));
        }

        [Test]
        public void A_row_carries_the_name_the_player_gave()
        {
            _intake.Receive(3, Hello("Android Pixel 7"), "AndroidPlayer(Pixel 7)");

            ConsoleLog log = _intake.Receive(3, Row(0, 0), "AndroidPlayer(Pixel 7)");

            Assert.AreEqual("Android Pixel 7", log.Player);
            Assert.AreEqual("row", log.Message);
        }

        /// <summary>A row from a player that never said hello still says where it came from.</summary>
        [Test]
        public void A_row_before_any_hello_carries_the_connections_name()
        {
            ConsoleLog log = _intake.Receive(3, Row(0, 0), "AndroidPlayer(Pixel 7)");

            Assert.AreEqual("AndroidPlayer(Pixel 7)", log.Player);
        }

        [Test]
        public void With_no_name_at_all_the_id_is_the_name()
        {
            Assert.AreEqual("Player 3", _intake.NameOf(3));
            Assert.AreEqual("Player 3", _intake.Receive(3, Row(0, 0), null).Player);
        }

        /// <summary>
        /// Both ends count flows from one. Without the offset a device's flow 4 would fold into
        /// the editor's flow 4 in Flow mode, and a parent of 0 has to stay 0 - it means a root.
        /// </summary>
        [Test]
        public void Flow_ids_are_moved_out_of_the_editors_range()
        {
            ConsoleLog root = _intake.Receive(3, Row(4, 0), null);
            ConsoleLog child = _intake.Receive(3, Row(5, 4), null);
            ConsoleLog outside = _intake.Receive(3, Row(0, 0), null);

            Assert.AreEqual(PlayerLogIntake.FlowIdOffset + 4, root.FlowId);
            Assert.AreEqual(0, root.ParentFlowId);
            Assert.AreEqual(PlayerLogIntake.FlowIdOffset + 5, child.FlowId);
            Assert.AreEqual(PlayerLogIntake.FlowIdOffset + 4, child.ParentFlowId);
            Assert.AreEqual(0, outside.FlowId);
        }

        [Test]
        public void Disconnecting_forgets_the_player()
        {
            _intake.Receive(3, Hello("Android Pixel 7"), null);

            _intake.Disconnected(3);

            Assert.IsFalse(_intake.IsFlowPlayerPresent);
            Assert.AreEqual("Player 3", _intake.NameOf(3));
        }

        [Test]
        public void Bytes_that_are_not_an_envelope_record_nothing()
        {
            Assert.IsNull(_intake.Receive(3, new byte[] {1, 2, 3}, null));
            Assert.IsNull(_intake.Receive(3, null, null));
        }

        /// <summary>
        /// A recompile empties every static in the editor, the intake with them, while the player
        /// stays connected and never says hello again. What it knew is written out and read back
        /// around the reload, so the rows after it are still named and the echo still dropped.
        /// </summary>
        [Test]
        public void What_it_knows_survives_a_round_trip_through_text()
        {
            _intake.Receive(3, Hello("Android Pixel 7"), null);
            _intake.Receive(5, Hello("Android OnePlus 15"), null);

            var reloaded = new PlayerLogIntake();
            reloaded.Restore(_intake.Serialize(), new[] {3, 5});

            Assert.IsTrue(reloaded.IsFlowPlayerPresent);
            Assert.AreEqual("Android Pixel 7", reloaded.NameOf(3));
            Assert.AreEqual("Android OnePlus 15", reloaded.NameOf(5));
        }

        [Test]
        public void A_player_gone_during_the_reload_is_not_restored()
        {
            _intake.Receive(3, Hello("Android Pixel 7"), null);

            var reloaded = new PlayerLogIntake();
            reloaded.Restore(_intake.Serialize(), new int[0]);

            Assert.IsFalse(reloaded.IsFlowPlayerPresent);
            Assert.AreEqual("Player 3", reloaded.NameOf(3));
        }

        [Test]
        public void Nothing_and_nonsense_restore_to_nothing()
        {
            _intake.Restore(null, new[] {3});
            _intake.Restore("", new[] {3});
            _intake.Restore("not:a:record;;3", new[] {3});

            Assert.IsFalse(_intake.IsFlowPlayerPresent);
        }

        [Test]
        public void A_name_with_the_separators_in_it_comes_back_whole()
        {
            _intake.Receive(3, Hello("Android Weird;Name=Yes"), null);

            var reloaded = new PlayerLogIntake();
            reloaded.Restore(_intake.Serialize(), new[] {3});

            Assert.AreEqual("Android Weird;Name=Yes", reloaded.NameOf(3));
        }
    }
}