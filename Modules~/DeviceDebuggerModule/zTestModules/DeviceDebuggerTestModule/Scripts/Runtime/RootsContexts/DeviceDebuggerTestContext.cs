#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Controllers;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Signals;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.RootsContexts
{
    /// <summary>
    /// The test scene's own module: a holder with every kind of option, and a command behind
    /// each. Press Play, tap the FlowIoC pill in the corner, and walk the five tabs.
    /// </summary>
    public class DeviceDebuggerTestContext : Context
    {
        private DeviceDebuggerTestSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            // Across contexts on purpose: the panel lists cross-context holders, and a test
            // module may take that liberty where a game module's public holder lives in Signals/.
            _signals = InjectionBinderCrossContext.Bind<DeviceDebuggerTestSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.LogLine).ToSequence<LogTestLineCommand>();

            CommandBinder.Bind(_signals.Incoming.SetGodMode).ToSequence<EchoGodModeCommand>();

            CommandBinder.Bind(_signals.Incoming.SetCoins).ToSequence<EchoCoinsCommand>();

            CommandBinder.Bind(_signals.Incoming.AddCoins).ToSequence<EchoCoinsCommand>();

            CommandBinder.Bind(_signals.Incoming.SetSpeed).ToSequence<LogSpeedCommand>();

            CommandBinder.Bind(_signals.Incoming.SetName).ToSequence<LogNameCommand>();

            CommandBinder.Bind(_signals.Incoming.SetMood).ToSequence<LogMoodCommand>();

            CommandBinder.Bind(_signals.Incoming.Throw).ToSequence<ThrowTestExceptionCommand>();

            CommandBinder.Bind(_signals.Incoming.Spam).ToSequence<SpamTestLogsCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            FlowLogger.LogWarning("A warning from the test scene, so the Warn filter has something to hide.");
            FlowLogger.LogError("An error logged on purpose by the test scene: the badge in the corner should read 1.");
        }
    }
}

#endif
