using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.BaseModule.Function
{
    /// <summary>
    /// What every function is built on, and what nothing outside FlowIoC derives from directly.
    /// The constructor is internal, so a game's function has to pick one of the shipped arities -
    /// a FunctionVoid, a FunctionReturn or an AsyncFunction - and the compiler is what says so.
    ///
    /// That is the whole reason for the internal constructor. A class written straight on this one
    /// has no typed Execute for the provider to call, so the provider used to find its method by
    /// name and invoke it through reflection: it worked, it cost a boxed call per run, and nothing
    /// reported that the function had been given the wrong base. Closing the door here let that
    /// fallback go.
    /// </summary>
    public abstract class FunctionBody : IFunctionBody
    {
        internal FunctionBody()
        {
        }

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

        /// <summary>
        /// Which execution of this pooled instance is the current one, the way a pooled command
        /// carries the same number. A function that retains and releases inside its own Execute is
        /// back in the pool before that Execute returns, and a nested call of the same type takes
        /// the same instance straight out again - so the frame that started the first run finds an
        /// instance whose flags belong to the second.
        /// </summary>
        internal int RunToken;

        /// <summary>
        /// Readies a pooled instance for one execution. Both retain flags belong to a single run:
        /// leaving either set carries the last run's answer into this one, and clearing them here
        /// rather than on the way out is what lets a function release itself mid-Execute without
        /// the run's own check then pooling it a second time.
        /// </summary>
        internal void BeginRun()
        {
            IsRetain = false;
            HasRetain = false;
            RunToken++;
        }

        /// <summary>
        /// Clears what the run left behind, as the function goes back to the pool. It does not
        /// touch <see cref="HasRetain"/>: a function that retained and released inside one Execute
        /// passes through here before the run is judged, and clearing the flag there would make the
        /// run look as though nothing had retained and pool the instance again.
        /// <see cref="BeginRun"/> is where the flag is cleared instead.
        /// </summary>
        public virtual void Dispose()
        {
            IsRetain = false;
        }

        /// <summary>
        /// Calls this function's own Execute with the parameters the caller lined up, without
        /// reflection. Every arity implements it, which is what makes the call typed - and it is
        /// abstract rather than virtual so that a new arity cannot be added without answering it
        /// and quietly run nothing.
        ///
        /// It answers true once it has dealt with the call, a reported bad argument included; false
        /// says this function is not run this way at all, which is what an AsyncFunction says.
        /// </summary>
        internal abstract bool TryInvokeExecute(object[] parameters, out object result);
    }

    public interface IFunctionBody
    {
        bool IsRetain { get; }
        bool HasRetain { get; }

        void Retain();
        void Release();
        void Dispose();
    }
}