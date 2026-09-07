using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;
using FlowIoC.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Probe
{
    /// <summary>
    /// Stands in for a game's own command. It has to live outside the FlowIoC namespace,
    /// because that namespace is exactly what the frame filter steps over.
    /// </summary>
    public class ReleasingWithoutRetainCommand : Command
    {
        public override void Execute()
        {
            Release();
        }
    }

    /// <summary>
    /// Retains and parks itself, so a test can release it from its own frame - which is what
    /// puts the command outside the stack of the diagnostic that follows.
    /// </summary>
    public class RetainingProbeCommand : Command
    {
        internal static RetainingProbeCommand Last;

        public override void Execute()
        {
            Retain();
            Last = this;
        }
    }
}

namespace FlowIoC.Tests
{
    /// <summary>
    /// A FlowIoC diagnostic is written by the framework but caused by the game, and the log has
    /// to point at the game. Before the frame filter this error's source was
    /// CommandGroupResolver - the framework checking a condition, not the code that broke it -
    /// and double-clicking it opened a file whose author had nothing to fix.
    /// </summary>
    public class DiagnosticSourceTests
    {
        [SetUp]
        public void ClearBefore() => FlowLogger.ClearLogs();

        [TearDown]
        public void ClearAfter() => FlowLogger.ClearLogs();

        [Test]
        public void A_diagnostic_points_at_the_game_code_that_caused_it()
        {
            LogAssert.Expect(LogType.Error,
                "Command must be retained to call manual RELEASE! Command: ReleasingWithoutRetainCommand");

            var binder = new CommandBinder {Context = new StandInContext()};
            var signal = new Signal();
            binder.Bind(signal).ToSequence<Game.Probe.ReleasingWithoutRetainCommand>();

            signal.Dispatch();

            ConsoleLog error = null;
            foreach (ConsoleLog log in FlowLogger.Logs)
            {
                if (log.LogType == LogType.Error && log.Message != null
                                                 && log.Message.Contains("manual RELEASE"))
                    error = log;
            }

            Assert.IsNotNull(error, "the retain guard wrote no error");
            Assert.IsNotNull(error.SourceClassName, "the error captured no source");
            StringAssert.StartsWith("Game.Probe", error.SourceClassName);
        }

        /// <summary>
        /// The case the frame filter cannot serve. The command released asynchronously, so by
        /// the time the guard fires the command is nowhere on the stack - the only frames are
        /// whoever called Release. The blamed type is what still names the file to open.
        /// </summary>
        [Test]
        public void A_diagnostic_names_its_type_even_when_that_type_is_not_on_the_stack()
        {
            LogAssert.Expect(LogType.Error,
                "Release was called on RetainingProbeCommand after its group finished.");

            var binder = new CommandBinder {Context = new StandInContext()};
            var signal = new Signal();
            binder.Bind(signal).ToSequence<Game.Probe.RetainingProbeCommand>();

            signal.Dispatch();

            Game.Probe.RetainingProbeCommand command = Game.Probe.RetainingProbeCommand.Last;
            Assert.IsNotNull(command, "the probe command did not run");

            command.Release();

            FlowLogger.ClearLogs();
            command.Release();

            ConsoleLog error = null;
            foreach (ConsoleLog log in FlowLogger.Logs)
            {
                if (log.LogType == LogType.Error)
                    error = log;
            }

            Assert.IsNotNull(error, "the detached-call guard wrote no error");
            Assert.AreEqual("Game.Probe.RetainingProbeCommand", error.BlameTypeName);
        }
    }
}