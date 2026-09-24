#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Controllers
{
    /// <summary>
    /// Takes the screen's layers away as displays. Their indicators go back to the layers' pools,
    /// and the targets wait, with their last content, for the screen to open again.
    /// </summary>
    internal class UnregisterSampleDisplaysCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }

        [SignalParam] private PointerSampleScreenView _view { get; set; }

        public override void Execute()
        {
            _worldPointerService.UnregisterDisplay(_view.HideLayer);
            _worldPointerService.UnregisterDisplay(_view.ClampLayer);
            _worldPointerService.UnregisterDisplay(_view.IgnoreLayer);
        }
    }
}
#endif
