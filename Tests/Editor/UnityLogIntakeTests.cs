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

        private const string ECHO = "<i>AndroidPlayer \"Pixel 7\"</i> Shared asset is not filed on any Root!";

        /// <summary>
        /// Unity's own forwarding re-logs a player's line in the editor as the player's name in
        /// italics and the text, with no trace. While a FlowIoC player is attached the same row
        /// has already arrived over our own message, so this copy is the echo.
        /// </summary>
        [Test]
        public void A_forwarded_player_line_is_an_echo_while_a_FlowIoC_player_is_attached()
        {
            Assert.IsTrue(_intake.IsPlayerEcho(ECHO, "", flowPlayerPresent: true));
            Assert.IsTrue(_intake.IsPlayerEcho(ECHO, null, flowPlayerPresent: true));
        }

        /// <summary>An older player, or an app without FlowIoC: the forwarded line is the only copy.</summary>
        [Test]
        public void The_same_line_is_kept_when_no_FlowIoC_player_is_attached()
        {
            Assert.IsFalse(_intake.IsPlayerEcho(ECHO, "", flowPlayerPresent: false));
        }

        [Test]
        public void A_line_with_a_trace_is_the_editors_own_and_kept()
        {
            Assert.IsFalse(_intake.IsPlayerEcho(ECHO, "Probe:Start() (at Assets/Probe.cs:3)", flowPlayerPresent: true));
        }

        [Test]
        public void A_line_that_does_not_open_with_a_player_name_is_kept()
        {
            Assert.IsFalse(_intake.IsPlayerEcho("<i>italic</i> but no player", "", flowPlayerPresent: true));
            Assert.IsFalse(_intake.IsPlayerEcho("plain", "", flowPlayerPresent: true));
            Assert.IsFalse(_intake.IsPlayerEcho(null, "", flowPlayerPresent: true));
        }
    }
}