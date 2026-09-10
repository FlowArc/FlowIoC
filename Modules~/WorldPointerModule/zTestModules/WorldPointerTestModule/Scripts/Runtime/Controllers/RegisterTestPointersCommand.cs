#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Entities;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>
    /// Pairs each cube with the indicator at the same index and hands the pairs to the service,
    /// after clearing whatever it was following. Each indicator brings its own preset.
    /// </summary>
    public class RegisterTestPointersCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }
        [InjectSignal] private WorldPointerTestInternalSignals _signals { get; set; }

        [SignalParam] private Transform[] _targets { get; set; }
        [SignalParam] private WorldPointerIndicator[] _indicators { get; set; }

        public override void Execute()
        {
            _worldPointerService.UnregisterAll();

            int count = 0;

            for (int i = 0; i < _targets.Length && i < _indicators.Length; i++)
            {
                WorldPointerIndicator indicator = _indicators[i];
                WorldPointerHandle handle = _worldPointerService.Register(_targets[i], indicator, indicator.Options);
                if (handle.IsValid) count++;
            }

            _signals.PointerCountChanged.Dispatch(count);
        }
    }
}
#endif
