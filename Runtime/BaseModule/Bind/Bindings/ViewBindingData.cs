using FlowIoC.BaseModule.Bind.Bindings.Mediator;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Bind.Bindings
{
    public struct ViewBindingData
    {
        public MediatorBinding Binding;
        public IContext Context;
    }
}