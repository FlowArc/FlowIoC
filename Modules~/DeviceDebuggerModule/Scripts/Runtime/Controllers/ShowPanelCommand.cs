using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Enums;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Which tab opens: the one asked for; for Last, the Console when an error waits unread, else
    /// whichever was open. Then the panel is open and the mediator is told.
    /// </summary>
    internal class ShowPanelCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [SignalParam] private DebugTab _tab { get; set; }

        public override void Execute()
        {
            if (!DeviceDebuggerConstants.IS_AVAILABLE) return;

            DebugTab tab = _tab;

            if (tab == DebugTab.Last)
                tab = _model.Logs.UnreadErrors > 0 ? DebugTab.Console : _model.ActiveTab;

            _model.SetActiveTab(tab);
            _model.SetOpen(true);

            // The badge counts what has not been looked at; the Console on screen is looking.
            if (tab == DebugTab.Console) _model.Logs.MarkErrorsRead();
        }
    }
}
