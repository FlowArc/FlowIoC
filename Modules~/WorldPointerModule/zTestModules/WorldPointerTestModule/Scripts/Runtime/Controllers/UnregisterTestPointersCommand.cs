#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>Unregisters every target; the screen's display gets every indicator back.</summary>
    internal class UnregisterTestPointersCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }
        [InjectSignal] private WorldPointerTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _worldPointerService.UnregisterAll();

            _signals.PointerCountChanged.Dispatch(0);
        }
    }
}
#endif