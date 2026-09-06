using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Injectable
{
    public class InjectionBinding : Binding
    {
        public string Name;
        public IContext BindedContext;

        /// <summary>
        /// Bindings are pooled, so everything the last use wrote has to go. Name and BindedContext
        /// used to survive: BindInstance sets no context, so a recycled binding kept pointing at
        /// whoever created it last, and injection then resolved that binding's members against
        /// another module's context.
        /// </summary>
        public override void Clear()
        {
            Name = "";
            BindedContext = null;
            base.Clear();
        }
    }
}