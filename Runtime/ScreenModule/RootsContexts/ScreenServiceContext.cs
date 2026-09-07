using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Commands;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.Signals;

namespace FlowIoC.ScreenModule.RootsContexts
{
    /// <summary>
    /// The screen service's own context, declared in the same four phases every module's context
    /// is. It used to do all of this in CoreBindings, which runs when the context starts whatever
    /// the Root's phase switches say - the one context in the package that did not keep to the
    /// rule the package asks of everyone else.
    /// </summary>
    internal class ScreenServiceContext : Context
    {
        private ScreenServiceInternalSignals _screenServiceInternalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _screenServiceInternalSignals = InjectionBinderCrossContext.Bind<ScreenServiceInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinderCrossContext.Bind<IScreenService, ScreenService>();
            InjectionBinder.Bind<IScreenRegistryModel, ScreenRegistryModel>();
            InjectionBinder.Bind<IScreenRuntimeModel, ScreenRuntimeModel>();

            InjectionBinder.Bind<AddressableLoadSubService>();
            InjectionBinder.Bind<ResourceLoadSubService>();
            InjectionBinder.Bind<DisposeSubService>();

            InjectionBinder.Bind<ScreenBuilderSubService>();
            InjectionBinder.Bind<ShowSubService>();
            InjectionBinder.Bind<SetupSubService>();
            InjectionBinder.Bind<LoadSubService>();
            InjectionBinder.Bind<CheckSubService>();
            InjectionBinder.Bind<TryGetSubService>();
            InjectionBinder.Bind<HideSubService>();
            InjectionBinder.Bind<UnloadSubService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_screenServiceInternalSignals.RegisterManager).ToSequence<RegisterScreenManagerCommand>();
            CommandBinder.Bind(_screenServiceInternalSignals.RegisterScreen).ToSequence<RegisterScreenCommand>();
            CommandBinder.Bind(_screenServiceInternalSignals.UnRegisterScreen).ToSequence<UnRegisterScreenCommand>();
        }
    }
}
