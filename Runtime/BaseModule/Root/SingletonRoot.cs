using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Root
{
    public abstract class SingletonRoot<TContextType> : Root<TContextType>
        where TContextType : IContext, new()
    {
        protected override bool CanCreateContext()
        {
            if (_rootsManager.SingletonRootRegistry.TryClaim(this))
                return true;

            FlowLogger.LogWarning(
                SystemLogType.Context,
                GetType().Name + " | A singleton Root of this type is already live. Removing the duplicate."
            );

            Destroy(this);
            return false;
        }

        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            base.BeforeCreateContext();
        }

        protected override void DestroyContext()
        {
            _rootsManager.SingletonRootRegistry.Release(this);

            base.DestroyContext();
        }
    }
}