using FlowIoC.BaseModule.Bind.Bindings;

namespace FlowIoC.BaseModule.Bind.Binders
{
    public interface IBinder<out TBindingType>
        where TBindingType : IBinding, new()
    {
        TBindingType Bind<TKeyType>();
        TBindingType Bind(object key);
        void UnBind(object key);
        void UnBindAll();

        TBindingType GetBinding(object key);
        TBindingType GetBinding<TKeyType>();
    }
}