using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ScreenModule.Data;

namespace FlowIoC.ScreenModule.Signals
{
    internal class ScreenServiceInternalSignals : ISignalHolder
    {
        public Signal<ScreenManagerVO, IContext> RegisterManager = new();
        public Signal<int, List<CD_Screen>> RegisterConfigs = new();
        public Signal<int, List<CD_Screen>> UnRegisterConfigs = new();
    }
}