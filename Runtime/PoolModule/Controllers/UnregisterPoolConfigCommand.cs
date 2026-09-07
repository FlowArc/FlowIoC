using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;
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
                FlowLogger.LogError(SystemLogType.Pool, "[PoolService][UnregisterPoolConfigCommand] Cannot unregister null pool configs!");
                return;
            }

            foreach (var config in _configs)
            {
                if (config.Value == null)
                {
                    FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolService][UnregisterPoolConfigCommand] Null config found in pool configs! key:{config.Key}");
                    continue;
                }

                _poolConfigModel.UnregisterPoolConfig(config);
            }
        }
    }
}
