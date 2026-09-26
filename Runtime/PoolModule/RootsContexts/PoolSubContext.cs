using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Data.ValueObjects;
using FlowIoC.PoolModule.Signals;
using UnityEngine;

namespace FlowIoC.PoolModule.RootsContexts
{
    /// <summary>
    /// Registers a module's pool groups from the module's own Root. List it on the Root of the
    /// module that spawns the objects and fill the entry's groups; the pool service's Root is not
    /// touched, the way a screen context sits on the Root that shows it.
    ///
    /// It registers in Setup, which runs once every Root has bound, so the service is there
    /// whichever Root started first, and a Launch step that fills a group finds it configured.
    /// The service's own command decides what registering means.
    /// </summary>
    public sealed class PoolSubContext : Context, ISubContextConfigurable<PoolSubContextSettingsCVO>
    {
        private PoolSubContextSettingsCVO _settings;
        private PoolServiceInternalSignals _poolSignals;
        private bool _registered;

        void ISubContextConfigurable.Configure(SubContextSettingsCVO settings)
        {
            _settings = settings as PoolSubContextSettingsCVO;
        }

        public override void Setup()
        {
            base.Setup();

            GameObject root = InjectionBinder.GetInstance<GameObject>(nameof(IContext));

            if (!InjectionBinderCrossContext.HasBinding<PoolServiceInternalSignals>())
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    $"[PoolSubContext.Setup][root({root.name})] could not reach the pool service. Is PoolServiceRoot in the scene?",
                    context: root);
                return;
            }

            if (_settings == null || _settings.Groups == null || _settings.Groups.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Pool,
                    $"[PoolSubContext.Setup][root({root.name})] lists no pool groups.");
                return;
            }

            _poolSignals = InjectionBinderCrossContext.GetInstance<PoolServiceInternalSignals>();

            FlowLogger.Log(SystemLogType.Pool, $"[PoolSubContext.Setup][root({root.name})][groups({_settings.Groups.Count})]");
            _poolSignals.RegisterPoolConfigs.Dispatch(_settings.Groups);
            _registered = true;
        }

        public override void DestroyContext()
        {
            // Before base, which takes the binders down. On quit the service may already be gone;
            // its commands are unbound then, and the dispatch reaches nothing.
            if (_registered && _settings.UnregisterWhenRootDestroyed)
                _poolSignals.UnRegisterConfigs.Dispatch(_settings.Groups);

            _registered = false;

            base.DestroyContext();
        }
    }
}
