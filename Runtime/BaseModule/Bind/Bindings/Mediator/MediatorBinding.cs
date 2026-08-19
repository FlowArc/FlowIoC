using FlowIoC.BaseModule.ViewsMediators.Mediator;

namespace FlowIoC.BaseModule.Bind.Bindings.Mediator
{
    public sealed class MediatorBinding : Binding
    {
        public new void To<TValueType>()
            where TValueType : IMediator
        {
            Value = typeof(TValueType);
        }

        public void To(IMediator mediator)
        {
            Value = mediator;
        }
    }
}