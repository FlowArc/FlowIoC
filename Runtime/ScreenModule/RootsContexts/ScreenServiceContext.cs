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
    internal class ScreenServiceContext : Context
    {
        private ScreenServiceInternalSignals _screenServiceInternalSignals;

        protected override void CoreBindings()
        {
            base.CoreBindings();

            _screenServiceInternalSignals = InjectionBinderCrossContext.Bind<ScreenServiceInternalSignals>();
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

            CommandBinder.Bind(_screenServiceInternalSignals.RegisterManager).ToSequence<RegisterScreenManagerCommand>();
            CommandBinder.Bind(_screenServiceInternalSignals.RegisterScreen).ToSequence<RegisterScreenCommand>();
            CommandBinder.Bind(_screenServiceInternalSignals.UnRegisterScreen).ToSequence<UnRegisterScreenCommand>();
        }
    }
}