using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.PoolModule.Models.Config;
using FlowIoC.PoolModule.Models.Runtime;

namespace FlowIoC.PoolModule.Services.Sub
{
    /// <summary>
    /// Questions about a group. They answer and say nothing more: a caller asking whether a group
    /// is ready is usually about to make it ready, and used to be warned for asking.
    /// </summary>
    public class CheckSubService
    {
        [Inject] private IPoolRuntimeModel _runtimeModel { get; set; }
        [Inject] private IPoolConfigModel _configModel { get; set; }

        public bool IsGroupReady(string group) => IsGroupConfigExist(group) && IsGroupCreated(group);

        public bool IsGroupConfigExist(string group) => _configModel.IsGroupConfigExist(group);

        public bool IsGroupCreated(string group) => _runtimeModel.IsGroupCreated(group);
    }
}
