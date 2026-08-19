using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.BaseModule.Function
{
    public class FunctionBody : IFunctionBody
    {
        [Inject] protected IFunctionProvider _functionProvider { get; set; }
        
        public bool IsRetain { get; set; }
        public bool HasRetain { get; set; }

        public virtual void Retain()
        {
            IsRetain = true;
            HasRetain = true;
        }

        public virtual void Release()
        {
            if (_functionProvider is FunctionProvider provider)
            {
                provider.ReleaseFunctionManually(this);
            }
        }

        public virtual void Dispose()
        {
            IsRetain = false;
            HasRetain = false;
        }
    }
    
    public interface IFunctionBody
    {
        bool IsRetain { get; set; }
        bool HasRetain { get; set; }
        
        void Retain();
        void Release();
        void Dispose();
    }
}