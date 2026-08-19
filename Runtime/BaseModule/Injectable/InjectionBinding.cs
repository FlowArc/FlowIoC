using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Injectable
{
    public class InjectionBinding : Binding
    {
        public string Name;
        public IContext BindedContext;
    }
}