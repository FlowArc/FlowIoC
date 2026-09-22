using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;
using Modules.BotBarModule.Signals;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>
    /// Runs at Launch, after every Connector's Setup has asked for its keys: a key no tab carries is
    /// a typo in a Connector, and a signal nobody will ever dispatch, so it is named here rather
    /// than found by a tab that does nothing.
    /// </summary>
    internal class WarnUnknownKeysCommand : Command
    {
        [Inject] private IBotBarModel _model { get; set; }
        [InjectSignal] private BotBarSignals _signals { get; set; }

        public override void Execute()
        {
            foreach (string key in _signals.Outgoing.RequestedKeys)
            {
                if (!_model.HasTab(key))
                    FlowLogger.LogWarning($"BotBar - a Connector connected tab '{key}', which CD_BotBar does not list.");
            }
        }
    }
}
