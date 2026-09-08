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

        /// <summary>
        /// The failure this exists for. A compile error arrives twice - once through
        /// CompilationPipeline carrying the file and the line, and once as text through
        /// Application.logMessageReceived carrying neither - so one error read as two rows and the
        /// second had nowhere to go when it was double-clicked.
        /// </summary>
        [Test]
        public void A_compile_error_Unity_is_printing_is_recognised()
        {
            Assert.IsTrue(_intake.IsCompilerMessage(
                "Assets/Modules/MainModule/Scripts/Runtime/Controllers/AaaFunction.cs(8,26): "
                + "error CS0246: The type or namespace name 'NewInjectable' could not be found"));
        }

        [Test]
        public void A_compile_warning_is_recognised_too()
        {
            Assert.IsTrue(_intake.IsCompilerMessage(
                "Assets/A.cs(12,5): warning CS0168: The variable 'x' is declared but never used"));
        }

        [Test]
        public void An_ordinary_message_is_not_a_compiler_one()
        {
            Assert.IsFalse(_intake.IsCompilerMessage("Opened MainScreen"));
            Assert.IsFalse(_intake.IsCompilerMessage("Damage (12,5): applied"));
            Assert.IsFalse(_intake.IsCompilerMessage(null));
        }
    }
}