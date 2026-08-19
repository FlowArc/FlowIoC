using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Signals;

namespace FlowIoC.PoolModule.ViewsMediators.ConfigAdapter
{
    internal class PoolConfigAdapterMediator : IMediator
    {
        [Inject] private PoolConfigAdapterView _view { get; set; }
        [InjectSignal] private PoolServiceSignals _poolServiceSignals { get; set; }

        public void OnRegister()
        {
            RegisterPoolConfig();
        }

        public void OnRemove()
        {
            UnregisterPoolConfig();
        }

        private void RegisterPoolConfig()
        {
            var configs = _view.GetPoolConfigs();
            if (configs == null || configs.Count == 0)
                FlowLogger.LogWarning(SystemLogType.Pool, $"[PoolConfigAdapterMediator.RegisterPoolConfig] No pool configs!");
            else
            {
                FlowLogger.Log(SystemLogType.Pool, $"[PoolConfigAdapterMediator.RegisterPoolConfig] Registering {configs.Count} pool configs");
                _poolServiceSignals.RegisterPoolConfigs.Dispatch(configs);
            }
        }

        private void UnregisterPoolConfig()
        {
            if(!_view.UnregisterWhenViewDestroyed) return;
            
            var configs = _view.GetPoolConfigs();
            if (configs == null || configs.Count == 0)
                FlowLogger.LogWarning(SystemLogType.Pool, "[PoolConfigAdapterMediator.UnregisterPoolConfig] No pool configs!");
            else
            {
                FlowLogger.Log(SystemLogType.Pool, $"[PoolConfigAdapterMediator.UnregisterPoolConfig] Unregistering {configs.Count} pool configs");
                _poolServiceSignals.UnRegisterConfigs.Dispatch(configs);
            }
        }
    }
}