#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.Shared.Constants;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Controllers
{
    /// <summary>
    /// Makes the screen's three layers the displays of the sample's three ids. Every target
    /// already registered under them is given an indicator at once, with its last content.
    /// </summary>
    internal class RegisterSampleDisplaysCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }

        [SignalParam] private WorldPointerSampleScreenView _view { get; set; }

        public override void Execute()
        {
            _worldPointerService.RegisterDisplay(WorldPointerSampleIds.Hide, _view.HideLayer);
            _worldPointerService.RegisterDisplay(WorldPointerSampleIds.Clamp, _view.ClampLayer);
            _worldPointerService.RegisterDisplay(WorldPointerSampleIds.Ignore, _view.IgnoreLayer);
        }
    }
}
#endif
