using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.UnityObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>
    /// What the mediator paints from: the model's live data, handed over on a signal whenever a
    /// Command changed something. A Mediator injects its View and signals and nothing else, so
    /// this is how the model reaches it - the ring and the lists are the model's own objects,
    /// read on the view's tick, never copied.
    /// </summary>
    public class PanelStateVO
    {
        public CD_DeviceDebugger Config;
        public LogRing Logs;
        public IReadOnlyList<DebugOptionVO> Options;
        public IReadOnlyList<DebugSignalVO> Signals;
        public IReadOnlyList<InfoRowVO> Info;

        /// <summary>Every channel the logger knows, for the Console's Filters panel.</summary>
        public IReadOnlyList<FlowLogChannel> Channels;
        public DebugTab ActiveTab;
        public bool IsOpen;
        public int OptionsVersion;
    }
}
