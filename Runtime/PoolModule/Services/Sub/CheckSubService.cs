using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;

namespace FlowIoC.PoolModule.Services.Sub
{
    public class CheckSubService
    {
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private IPoolConfigModel _configModel { get; set; }

        public bool IsGroupReady(string group) => IsGroupConfigExist(group) && IsGroupCreated(group);

        public bool IsGroupConfigExist(string group)
        {
            if (_configModel.IsGroupConfigExist(group)) return true;
            
            FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolService.Check] Group '{group}' config not found.");
            return false;
        }
        public bool IsGroupCreated(string group)
        {
            if (_runtimeModel.IsGroupCreated(group)) return true;
            
            FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolService.Check] Group '{group}' is not registered.");
            return false;
        }

    }
} 