using System;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;

namespace FlowIoC.ScreenModule.Signals
{
    internal class ScreenServiceInternalSignals : ISignalHolder
    {
        public Signal<ScreenManagerVO> RegisterManager = new();
        public Signal<ScreenEntry> RegisterScreen = new();
        public Signal<int, Type> UnRegisterScreen = new();
    }
}