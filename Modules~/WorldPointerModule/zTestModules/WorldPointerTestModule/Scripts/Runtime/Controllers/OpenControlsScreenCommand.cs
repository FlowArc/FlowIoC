#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.WorldPointerModule.PointerControlsScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>Opens the controls screen - the sample's buttons - once the scene is up.</summary>
    internal class OpenControlsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }

        public override async void Execute()
        {
            Retain();

            try
            {
                var screen = await _screenService.Open<PointerControlsScreenView>()
                    .Show<PointerControlsScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("OpenControlsScreenCommand - the controls screen did not open.");
                    Stop();
                    return;
                }

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenControlsScreenCommand threw while opening the controls screen: {exception}");
                Stop();
            }
        }
    }
}
#endif
