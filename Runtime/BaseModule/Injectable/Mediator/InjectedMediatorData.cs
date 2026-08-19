using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;

namespace FlowIoC.BaseModule.Injectable.Mediator
{
    public class InjectedMediatorData
    {
        public IView view;
        public IMediator mediator;
        public ViewInjector viewInjector;
    }
}