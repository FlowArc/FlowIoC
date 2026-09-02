using FlowIoC.BaseModule.Root;

namespace FlowIoC.AssetModule.RootsContexts
{
    internal class AssetServiceRoot : Root<AssetServiceContext>
    {
        /// <summary>
        /// The asset service holds the handles of every loaded Addressable group, so it has to
        /// outlive the scene that first asked for one. Nothing in the framework keeps a Root
        /// alive across a load, so this one says so itself.
        /// </summary>
        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}