using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace FlowIoC.AssetModule.RootsContexts
{
    [CustomClassHeader("ROOTs", 0.8f, 0.2f, 0.2f, 0.2f, 0.2f, 0.8f, 14)]
    internal class AssetServiceRoot : SingletonRoot<AssetServiceContext>
    {
    }
}
