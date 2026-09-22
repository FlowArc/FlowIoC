using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Shared.Enums;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>
    /// The game decided a tab is locked or not; the bar shows it. Locking the tab the player is on
    /// moves them to StartTab through the same path a tap takes, unless StartTab is locked too.
    /// </summary>
    internal class SetLockedCommand : Command
    {
        [Inject] private IBotBarModel _model { get; set; }
        [InjectSignal] private BotBarSignals _signals { get; set; }
        [InjectSignal] private BotBarInternalSignals _internalSignals { get; set; }

        [SignalParam] private string _key { get; set; }
        [SignalParam] private bool _locked { get; set; }

        public override void Execute()
        {
            if (!_model.HasTab(_key))
            {
                FlowLogger.LogWarning($"SetLocked - no tab keyed '{_key}' in CD_BotBar.");
                return;
            }

            if (!_model.SetLocked(_key, _locked, out BotBarTabState state))
                return;

            _signals.Outgoing.TabStateChanged.Dispatch(_key, state);
            FlowLogger.Log($"Execute - SetLockedCommand | {_key} {state}");

            if (!_locked || _key != _model.Selected)
                return;

            if (_model.GetTab(_model.StartTab).State == BotBarTabState.Locked)
            {
                FlowLogger.LogWarning($"SetLocked - '{_key}' was selected and StartTab '{_model.StartTab}' is locked too; the selection stays.");
                return;
            }

            _internalSignals.SelectTab.Dispatch(_model.StartTab);
        }
    }
}
