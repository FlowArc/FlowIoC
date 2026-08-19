using FlowIoC.BaseModule.Signals;
using FlowIoC.PoolModule.Entities;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Signals
{
    public class PoolServiceSignals : ISignalHolder
    {
        public Signal<SerializedDictionary<string, PoolGroupCVO>> RegisterPoolConfigs = new();
        public Signal<SerializedDictionary<string, PoolGroupCVO>> UnRegisterConfigs = new();
    }
}