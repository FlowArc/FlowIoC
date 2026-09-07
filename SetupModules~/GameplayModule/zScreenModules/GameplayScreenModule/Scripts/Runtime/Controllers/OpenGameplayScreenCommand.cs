using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.GameplayScreenModule.Controllers
{
    internal class OpenGameplayScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }

        [SignalParam] private DifficultyType _difficulty { get; set; }

        /// <summary>
        /// Three ways out, and every one of them resolves the retain: the screen opened, the screen
        /// came back null, and the await threw. A retain nobody resolves hangs the group for ever,
        /// with no timeout and nothing logged.
        /// </summary>
        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(FlowLogType.GameplayScreenModule,
                $"Execute - OpenGameplayScreenCommand | difficulty={_difficulty}");

            try
            {
                GameplayScreenView screen = await _screenService.Open<GameplayScreenView>()
                    .SetParameters(_difficulty)
                    .Show<GameplayScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError(FlowLogType.GameplayScreenModule,
                        "OpenGameplayScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }

                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.GameplayScreenModule,
                    $"OpenGameplayScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
