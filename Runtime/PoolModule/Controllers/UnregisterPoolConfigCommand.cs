using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Config;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Controllers
{
    public sealed class UnregisterPoolConfigCommand : Command
    {
        [SignalParam] private SerializedDictionary<string, PoolGroupCVO> _configs { get; set; }
        [Inject] private IPoolConfigModel _poolConfigModel { get; set; }

        public override void Execute()
        {
            if (_configs == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[PoolService][RegisterPoolConfigCommand] Cannot register null screen configs!");
                return;
            }

            foreach (var config in _configs)
            {
                if (config.Value == null)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen, $"[PoolService][RegisterPoolConfigCommand] Null config found in screen configs list! index:{config}");
                    continue;
                }

                _poolConfigModel.UnregisterPoolConfig(config);
            }
        }
    }
}