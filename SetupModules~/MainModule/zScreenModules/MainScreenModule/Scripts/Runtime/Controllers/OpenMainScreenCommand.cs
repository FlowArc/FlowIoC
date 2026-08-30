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

        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(FlowLogType.MainScreenModule, $"{nameof(Execute)} - {nameof(OpenMainScreenCommand)}");

            await _screenService.Open<MainScreenView>().Show();

            Release();
        }
    }
}
