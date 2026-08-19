using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Root
{
    public abstract class SingletonRoot<TContextType> : Root<TContextType>
        where TContextType : IContext, new()
    {
        protected override void BeforeCreateContext()
        {
            if (!SingletonRootRegistry.TryClaim(this))
            {
                AutoInitialize = false;
                Destroy(gameObject);
                return;
            }

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            base.BeforeCreateContext();
        }

        protected override void DestroyContext()
        {
            if (!SingletonRootRegistry.IsClaimedBy(this))
            {
                _rootsManager?.UnRegister(this);
                return;
            }

            SingletonRootRegistry.Release(this);
            base.DestroyContext();
        }
    }
}
