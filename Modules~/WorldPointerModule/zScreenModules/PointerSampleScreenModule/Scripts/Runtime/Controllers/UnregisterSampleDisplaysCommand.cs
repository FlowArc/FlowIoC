#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Constants;
using Modules.WorldPointerModule.Services;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Controllers
{
    /// <summary>
    /// Takes the sample's three channels' displays away. Their labels go back to the pool, and the
    /// targets wait, with their last content, for the screen to open again.
    /// </summary>
    internal class UnregisterSampleDisplaysCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }

        public override void Execute()
        {
            _worldPointerService.UnregisterDisplay(WorldPointerSampleIds.Hide);
            _worldPointerService.UnregisterDisplay(WorldPointerSampleIds.Clamp);
            _worldPointerService.UnregisterDisplay(WorldPointerSampleIds.Ignore);
        }
    }
}
#endif