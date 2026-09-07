using FlowIoC.BaseModule.Signals;
using FlowIoC.PoolModule.Data.ValueObjects;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Signals
{
    internal class PoolServiceInternalSignals : ISignalHolder
    {
        public Signal<SerializedDictionary<string, PoolGroupCVO>> RegisterPoolConfigs = new();
        public Signal<SerializedDictionary<string, PoolGroupCVO>> UnRegisterConfigs = new();
    }
}