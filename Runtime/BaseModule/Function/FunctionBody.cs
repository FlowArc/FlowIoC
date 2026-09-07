using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.BaseModule.Function
{
    public class FunctionBody : IFunctionBody
    {
        [Inject] protected IFunctionProvider _functionProvider { get; set; }

        public bool IsRetain { get; set; }
        public bool HasRetain { get; set; }

        /// <summary>
        /// The context this instance was last filled from and the binding generation it was filled
        /// at, the way a pooled command remembers them: a function is called from inside a command
        /// and runs as often as one, so its members are resolved again only once one of these moves.
        /// </summary>
        internal IContext InjectedContext;

        internal int InjectionStamp = -1;

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

        /// <summary>
        /// Calls this function's own Execute with the parameters the caller lined up, without
        /// reflection. Each arity overrides it; a function written straight on FunctionBody has
        /// no typed Execute for the provider to reach, so it answers false and the provider finds
        /// the method by name the way it always did.
        /// </summary>
        internal virtual bool TryInvokeExecute(object[] parameters, out object result)
        {
            result = null;
            return false;
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
