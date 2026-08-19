using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Services;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Controllers
{
    public class RegisterPoolConfigCommand : Command
    {
        [SignalParam] private SerializedDictionary<string, PoolGroupCVO> _configs { get; set; }
        [Inject] private IPoolConfigModel _poolConfigModel { get; set; }
        [Inject] private IPoolService _pool { get; set; }

        public override void Execute()
        {
            if (_configs == null)
            {
                FlowLogger.LogError(SystemLogType.Pool, "[PoolService][RegisterPoolConfigCommand] Cannot register null screen configs!");
                return;
            }

            FlowLogger.Log(SystemLogType.Pool,$"[PoolService][RegisterPoolConfigCommand] Registering pool configs: {_configs?.Count}");
            foreach (var config in _configs)
            {
                if (config.Value == null)
                {
                    FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolService][RegisterPoolConfigCommand] Null config found in screen configs list! index:{config}");
                    continue;
                }

                _poolConfigModel.RegisterPoolConfig(config);
                
                //TODO: manual initialize
                //_pool.InitializeGroup();
            }
            _pool.AutoInitializeAll();
        }
    }
}