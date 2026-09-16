using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Enums;
using Modules.DeviceDebuggerModule.Models;
using Modules.DeviceDebuggerModule.Signals;

namespace Modules.DeviceDebuggerModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through the internal signals, so each one is a
    /// step the Flow Console shows and the decisions - which tab, whether the panel exists in this
    /// build - are taken in a Command. PostConstruct starts the log capture: it runs during the
    /// module's own binding pass, so every Root bound after this one has its binding lines in
    /// the ring, and a boot that fails on the phone is readable without a cable.
    /// </summary>
    public class DeviceDebuggerService : IDeviceDebuggerService, IConstructable
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [InjectSignal] private DeviceDebuggerInternalSignals _signals { get; set; }

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public bool IsAvailable => DeviceDebuggerConstants.IS_AVAILABLE;

        public bool IsOpen => _model.IsOpen;

        public void PostConstruct() => _signals.StartCapture.Dispatch();

        public void Show() => _signals.Show.Dispatch(DebugTab.Last);

        public void Show(DebugTab tab) => _signals.Show.Dispatch(tab);

        public void Hide() => _signals.Hide.Dispatch();

        public void Toggle() => _signals.Toggle.Dispatch();
    }
}
