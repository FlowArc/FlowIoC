using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Service;
using Modules.LoadingModule.Services;
using Modules.MainModule.Constants;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// Loads every registered screen into the pool before the main screen opens, and reports the
    /// Screens step as it goes. The loading screens are already in - the boot's first step put them
    /// there, before the set began - so Load.All passes over them at once and the bar shows the
    /// rest. Load.All calls back rather than returning a Task, so the retain is resolved in the
    /// callback; a load that stops is reported by the screen service itself and the callback never
    /// comes - which the loading set's stall warning is there to say.
    /// </summary>
    internal class PreloadScreensCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILoadingService _loadingService { get; set; }

        public override void Execute()
        {
            Retain();

            ILoadingStep step = _loadingService.Report(MainConstants.SCREENS_STEP);
            step.Start();

            _screenService.Load.All(
                completeCallback: () =>
                {
                    step.Complete();
                    Release();
                },
                loadingProgressCallback: (index, count) =>
                {
                    step.Progress(count == 0 ? 1f : (index + 1f) / count);
                    step.Detail($"{index + 1} / {count}");
                });
        }
    }
}