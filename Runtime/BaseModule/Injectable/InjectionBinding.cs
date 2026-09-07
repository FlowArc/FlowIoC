using FlowIoC.BaseModule.Bind.Bindings;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Injectable
{
    public class InjectionBinding : Binding
    {
        public string Name;

        /// <summary>
        /// The context that created the instance, and the one its members are resolved against.
        /// Null for an instance handed in with BindInstance: nobody created it, so it is resolved
        /// against whichever context is injecting.
        /// </summary>
        public IContext BoundContext;

        /// <summary>
        /// Bindings are pooled, so everything the last use wrote has to go. Name and BoundContext
        /// used to survive: BindInstance sets no context, so a recycled binding kept pointing at
        /// whoever created it last, and injection then resolved that binding's members against
        /// another module's context.
        /// </summary>
        public override void Clear()
        {
            Name = "";
            BoundContext = null;
            base.Clear();
        }
    }
}
