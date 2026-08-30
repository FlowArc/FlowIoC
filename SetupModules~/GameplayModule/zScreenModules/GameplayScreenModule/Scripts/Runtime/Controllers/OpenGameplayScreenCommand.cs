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

        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(FlowLogType.GameplayScreenModule,
                $"{nameof(Execute)} - {nameof(OpenGameplayScreenCommand)} | difficulty={_difficulty}");

            await _screenService.Open<GameplayScreenView>().SetParameters(_difficulty).Show();

            Release();
        }
    }
}
