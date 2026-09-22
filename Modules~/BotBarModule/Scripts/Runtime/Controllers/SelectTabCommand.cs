using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using Modules.BotBarModule.Shared.Enums;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>
    /// The one decision in the module, reached by a tap on the bar and by SelectTab from the game
    /// alike. In order: the NEW mark goes, the bar animates, the game opens the page.
    /// </summary>
    internal class SelectTabCommand : Command
    {
        [Inject] private IBotBarModel _model { get; set; }
        [InjectSignal] private BotBarSignals _signals { get; set; }

        [SignalParam] private string _key { get; set; }

        public override void Execute()
        {
            BotBarTabRVO tab = _model.GetTab(_key);

            if (tab == null)
            {
                FlowLogger.LogWarning($"SelectTab - no tab keyed '{_key}' in CD_BotBar.");
                return;
            }

            if (tab.State == BotBarTabState.Locked)
            {
                _signals.Outgoing.LockedTabTapped.Dispatch(_key);
                return;
            }

            if (_key == _model.Selected)
                return;

            string previous = _model.Select(_key);

            if (_model.MarkSeen(_key))
                _signals.Outgoing.TabStateChanged.Dispatch(_key, BotBarTabState.Open);

            _signals.Outgoing.SelectionChanged.Dispatch(previous, _key);
            _signals.Outgoing.Selected(_key).Dispatch();
            FlowLogger.Log($"Execute - SelectTabCommand | {previous} -> {_key}");
        }
    }
}
