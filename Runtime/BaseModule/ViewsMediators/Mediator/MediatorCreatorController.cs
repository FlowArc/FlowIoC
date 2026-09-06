using System;
using FlowIoC.BaseModule.Pooling;

namespace FlowIoC.BaseModule.ViewsMediators.Mediator
{
    /// <summary>
    /// Hands out the Mediator for a View and takes it back when the View goes. A Mediator is
    /// reused, so whatever <c>OnRegister</c> did has to be undone in <c>OnRemove</c>: the View and
    /// the signal holders are injected again on the way out, but a subscription the Mediator made
    /// itself, or anything it wrote to a plain field, is still there when it comes back.
    /// </summary>
    public class MediatorCreatorController
    {
        // Guarded: a View unregistering twice would otherwise park one Mediator for two Views to
        // find, and both would then drive the same instance.
        private readonly TypePool<IMediator> _pool = new(guardDoubleReturn: true);

        public IMediator GetMediator(Type mediatorType)
        {
            return _pool.TryTake(mediatorType, out IMediator mediator)
                ? mediator
                : (IMediator) Activator.CreateInstance(mediatorType);
        }

        public void ReturnMediatorToPool(IMediator mediator)
        {
            if (mediator == null)
                return;

            _pool.Return(mediator.GetType(), mediator);
        }

        public void Clear() => _pool.Clear();
    }
}
