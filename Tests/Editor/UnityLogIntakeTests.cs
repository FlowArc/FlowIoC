using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class UnityLogIntakeTests
    {
        private readonly UnityLogIntake _intake = new UnityLogIntake();

        [Test]
        public void An_ordinary_Unity_message_is_recorded()
        {
            Assert.IsTrue(_intake.ShouldRecord(LogType.Log, weAreWritingToUnityConsole: false));
        }

        [Test]
        public void Our_own_echo_is_dropped()
        {
            Assert.IsFalse(_intake.ShouldRecord(LogType.Log, weAreWritingToUnityConsole: true));
        }

        /// <summary>
        /// The console offers three filters - Log, Warning, Error. An exception that stayed a
        /// LogType.Exception would answer to none of them and could not be hidden or found.
        /// </summary>
        [Test]
        public void An_exception_is_filed_as_an_error()
        {
            Assert.AreEqual(LogType.Error, _intake.ToLogType(LogType.Exception));
        }

        [Test]
        public void A_failed_assert_is_filed_as_an_error()
        {
            Assert.AreEqual(LogType.Error, _intake.ToLogType(LogType.Assert));
        }

        [Test]
        public void The_three_ordinary_kinds_are_left_alone()
        {
            Assert.AreEqual(LogType.Log, _intake.ToLogType(LogType.Log));
            Assert.AreEqual(LogType.Warning, _intake.ToLogType(LogType.Warning));
            Assert.AreEqual(LogType.Error, _intake.ToLogType(LogType.Error));
        }
    }
}
