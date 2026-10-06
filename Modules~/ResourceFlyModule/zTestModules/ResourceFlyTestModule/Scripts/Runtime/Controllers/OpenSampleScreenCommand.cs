#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Constants;
using Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.ViewsMediators;
using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.Controllers
{
    /// <summary>
    /// Opens the sample screen and, from the view Show returns, registers its three counters and
    /// sources - the way a game's opening Command fills a screen before anything flies into it.
    /// </summary>
    internal class OpenSampleScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private IResourceFlyService _resourceFly { get; set; }

        public override async void Execute()
        {
            Retain();

            try
            {
                var screen = await _screenService.Open<ResourceFlySampleScreenView>().Show<ResourceFlySampleScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("OpenSampleScreenCommand - the sample screen did not open.");
                    Stop();
                    return;
                }

                _resourceFly.RegisterCounter(ResourceFlySampleKeys.SCATTER, screen.ScatterCounter);
                _resourceFly.RegisterCounter(ResourceFlySampleKeys.DIRECT, screen.DirectCounter);
                _resourceFly.RegisterCounter(ResourceFlySampleKeys.CURVED, screen.CurvedCounter);

                _resourceFly.RegisterSource(ResourceFlySampleKeys.SCATTER_SOURCE, screen.ScatterSource);
                _resourceFly.RegisterSource(ResourceFlySampleKeys.DIRECT_SOURCE, screen.DirectSource);
                _resourceFly.RegisterSource(ResourceFlySampleKeys.CURVED_SOURCE, screen.CurvedSource);

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenSampleScreenCommand threw while opening the sample screen: {exception}");
                Stop();
            }
        }
    }
}
#endif
