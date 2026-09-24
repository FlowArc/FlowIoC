#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>
    /// Closes the sample screen when it is open and opens it when it is not - the screen side
    /// coming and going under targets that stay registered. Bound to the Launch signal too, where
    /// the screen is not open yet, so it opens.
    /// </summary>
    internal class ToggleSampleScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }

        public override async void Execute()
        {
            if (_screenService.Check.IsScreenActive<WorldPointerSampleScreenView>())
            {
                _screenService.Hide.Screen<WorldPointerSampleScreenView>();
                return;
            }

            Retain();

            try
            {
                var screen = await _screenService.Open<WorldPointerSampleScreenView>().Show<WorldPointerSampleScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("ToggleSampleScreenCommand - the sample screen did not open.");
                    Stop();
                    return;
                }

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"ToggleSampleScreenCommand threw while opening the sample screen: {exception}");
                Stop();
            }
        }
    }
}
#endif
