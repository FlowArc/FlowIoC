using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.MainModule.MainScreenModule.ViewsMediators;

namespace Modules.MainModule.MainScreenModule.Controllers
{
    internal class OpenMainScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }

        /// <summary>
        /// Three ways out, and every one of them resolves the retain. The success path is the one
        /// everybody writes; the screen coming back null and the await throwing are the two that
        /// hang the group for ever if they are forgotten, because a retain nobody resolves has no
        /// timeout and logs nothing.
        ///
        /// What Stop() does here is this game's answer and not the framework's - another game
        /// releases and carries on, or dispatches a signal that opens something else instead.
        /// </summary>
        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(FlowLogType.MainScreenModule, "Execute - OpenMainScreenCommand");

            try
            {
                // Show<T>() rather than Show(): a typed view compares against null through Unity's
                // own operator, and an IScreenBody does not.
                MainScreenView screen = await _screenService.Open<MainScreenView>().Show<MainScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError(FlowLogType.MainScreenModule,
                        "OpenMainScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.MainScreenModule,
                    $"OpenMainScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
