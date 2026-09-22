using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.BotBarModule.Controllers;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.RootsContexts
{
    /// <summary>
    /// Declares the module and nothing else. What a game tells the bar reads below in the order it
    /// runs; the screen hears the announcements through the shipped BotBarScreenConnectorSubContext.
    /// </summary>
    public class BotBarSystemContext : Context
    {
        private BotBarSignals _signals;
        private BotBarInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<BotBarSignals>();
            _internalSignals = InjectionBinder.Bind<BotBarInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IBotBarModel, BotBarModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.Open)
                .ToSequence<SelectStartTabCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.Opened);

            CommandBinder.Bind(_signals.Incoming.SelectTab).ToSequence<SelectTabCommand>();

            // The fall back after the selected tab was locked: the same step, from inside.
            CommandBinder.Bind(_internalSignals.SelectTab).ToSequence<SelectTabCommand>();

            CommandBinder.Bind(_signals.Incoming.SetLocked).ToSequence<SetLockedCommand>();

            CommandBinder.Bind(_signals.Incoming.SetBadge).ToSequence<SetBadgeCommand>();

            CommandBinder.Bind(_signals.Incoming.Show).ToSequence<SetShownCommand>(true);

            CommandBinder.Bind(_signals.Incoming.Hide).ToSequence<SetShownCommand>(false);

            CommandBinder.Bind(_internalSignals.Launched).ToSequence<WarnUnknownKeysCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            // Every Setup in the scene has run, so every Connector has asked for its keys.
            _internalSignals.Launched.Dispatch();
        }
    }
}