#if UNITY_EDITOR
using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.BotBarModule.BotBarTestModule.Controllers;
using Modules.BotBarModule.BotBarTestModule.Models;
using Modules.BotBarModule.BotBarTestModule.Signals;
using Modules.BotBarModule.BotBarTestModule.ViewsMediators;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.RootsContexts
{
    /// <summary>
    /// Every button is one line into the bar's Incoming, and every announcement the bar makes
    /// lands on the label - the keyed signals included, one per key of the shipped CD_BotBar, so
    /// the Connector shape a game writes is the shape this scene proves. A BaseScreenContext, so the
    /// ScreenManager under the test Root is mediated the way the generated screen test scenes do it.
    /// </summary>
    public class BotBarTestContext : BaseScreenContext
    {
        private const string GROUP = nameof(BotBarTestContext);
        private static readonly string[] Keys = {"home", "battle", "clan", "collection", "shop"};

        private BotBarTestInternalSignals _signals;
        private BotBarSignals _botBar;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<BotBarTestInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<BotBarTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<BotBarTestView>().To<BotBarTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.SelectShop).ToSequence<SelectShopCommand>();
            CommandBinder.Bind(_signals.SetClanLocked).ToSequence<SetClanLockedCommand>();
            CommandBinder.Bind(_signals.IncrementShopBadge).ToSequence<IncrementShopBadgeCommand>();
            CommandBinder.Bind(_signals.ClearShopBadge).ToSequence<ClearShopBadgeCommand>();
            CommandBinder.Bind(_signals.SetBarShown).ToSequence<SetBarShownCommand>();
            CommandBinder.Bind(_signals.Note).ToSequence<NoteCommand>();
        }

        // A test-only liberty: this context hears the bar's Outgoing here, the one phase that may
        // reach across modules, so the label shows the last announcement.
        public override void Setup()
        {
            base.Setup();

            _botBar = InjectionBinderCrossContext.GetInstance<BotBarSignals>();

            if (_botBar == null)
                return;

            foreach (string key in Keys)
            {
                string captured = key;
                _botBar.Outgoing.Selected(key).Connect(() => _signals.Note.Dispatch($"Selected[{captured}]"), GROUP);
            }

            _botBar.Outgoing.SelectionChanged.Connect((previous, current) => _signals.Note.Dispatch($"SelectionChanged {previous} -> {current}"),
                GROUP);
            _botBar.Outgoing.TabStateChanged.Connect((key, state) => _signals.Note.Dispatch($"TabStateChanged {key} {state}"), GROUP);
            _botBar.Outgoing.BadgeChanged.Connect((key, count) => _signals.Note.Dispatch($"BadgeChanged {key} {count}"), GROUP);
            _botBar.Outgoing.LockedTabTapped.Connect(key => _signals.Note.Dispatch($"LockedTabTapped {key}"), GROUP);
            _botBar.Outgoing.Shown.Connect(() => _signals.Note.Dispatch("Shown"), GROUP);
            _botBar.Outgoing.Hidden.Connect(() => _signals.Note.Dispatch("Hidden"), GROUP);
            _botBar.Outgoing.Opened.Connect(() => _signals.Note.Dispatch("Opened"), GROUP);
        }

        public override void Launch()
        {
            base.Launch();

            _botBar?.Incoming.Open.Dispatch();
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
#endif