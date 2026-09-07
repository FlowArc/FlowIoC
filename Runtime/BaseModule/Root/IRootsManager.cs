using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Root
{
    public interface IRootsManager
    {
        void Register(IRoot root);
        void UnRegister(IRoot root);
        IRoot GetRootByName(string name);
        void StartContexts();
        void Initialize();
    }
}