#if UNITY_EDITOR
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.ResourceFlySampleScreenModule.Constants;
using Modules.ResourceFlyModule.ResourceFlySampleScreenModule.Controllers;
using Modules.ResourceFlyModule.ResourceFlySampleScreenModule.Models;
using Modules.ResourceFlyModule.ResourceFlySampleScreenModule.Signals;
using Modules.ResourceFlyModule.ResourceFlySampleScreenModule.ViewsMediators;
using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.ResourceFlySampleScreenModule.RootsContexts
{
    /// <summary>
    /// ResourceFly's sample: a lane per ready-made motion, opened and filled by the test module.
    /// A press runs the three steps a game binds - reserve, save, fly - so the counter holds still
    /// until the icons land. Editor-only, loaded from Resources; listed on ResourceFlyTestRoot only.
    /// </summary>
    public class ResourceFlySampleScreenContext : ScreenSubContext<ResourceFlySampleScreenView, ResourceFlySampleScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 1,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Resource("ResourceFlySampleScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private ResourceFlySampleScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            InjectionBinderCrossContext.Bind<ResourceFlySampleScreenSignals>();
            _internalSignals = InjectionBinder.Bind<ResourceFlySampleScreenInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IResourceFlySampleModel, ResourceFlySampleModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.FlyScatter)
                .ToSequence<IResourceFlyService.Commands.Reserve>(ResourceFlySampleKeys.SCATTER)
                .ToSequence<SaveSampleGrantCommand>(ResourceFlySampleKeys.SCATTER)
                .ToSequence<IResourceFlyService.Commands.Fly>(new ResourceFlyRouteVO(ResourceFlySampleKeys.SCATTER_SOURCE,
                    ResourceFlySampleKeys.SCATTER));

            CommandBinder.Bind(_internalSignals.FlyDirect)
                .ToSequence<IResourceFlyService.Commands.Reserve>(ResourceFlySampleKeys.DIRECT)
                .ToSequence<SaveSampleGrantCommand>(ResourceFlySampleKeys.DIRECT)
                .ToSequence<IResourceFlyService.Commands.Fly>(new ResourceFlyRouteVO(ResourceFlySampleKeys.DIRECT_SOURCE,
                    ResourceFlySampleKeys.DIRECT));

            CommandBinder.Bind(_internalSignals.FlyCurved)
                .ToSequence<IResourceFlyService.Commands.Reserve>(ResourceFlySampleKeys.CURVED)
                .ToSequence<SaveSampleGrantCommand>(ResourceFlySampleKeys.CURVED)
                .ToSequence<IResourceFlyService.Commands.Fly>(new ResourceFlyRouteVO(ResourceFlySampleKeys.CURVED_SOURCE,
                    ResourceFlySampleKeys.CURVED));
        }
    }
}
#endif
