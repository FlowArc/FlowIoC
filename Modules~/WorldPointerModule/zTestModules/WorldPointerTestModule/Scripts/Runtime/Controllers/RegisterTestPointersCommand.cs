#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Constants;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Data.ValueObjects;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>
    /// Registers each cube under the id at the same index and sends it a first label, after
    /// clearing whatever the service held. The world side stops there: whether the sample screen
    /// is open to draw them is not its business, and a label sent while it is closed is shown
    /// when it opens.
    /// </summary>
    internal class RegisterTestPointersCommand : Command
    {
        private static readonly string[] Ids = {WorldPointerSampleIds.Hide, WorldPointerSampleIds.Clamp, WorldPointerSampleIds.Ignore};
        private static readonly Color[] Colours = {new(0.25f, 0.7f, 0.35f), new(0.95f, 0.55f, 0.15f), new(0.25f, 0.5f, 0.95f)};

        [Inject] private IWorldPointerService _worldPointerService { get; set; }
        [InjectSignal] private WorldPointerTestInternalSignals _signals { get; set; }

        [SignalParam] private Transform[] _targets { get; set; }

        public override void Execute()
        {
            _worldPointerService.UnregisterAll();

            int count = 0;

            for (int i = 0; i < _targets.Length && i < Ids.Length; i++)
            {
                if (!_worldPointerService.RegisterTarget(Ids[i], _targets[i]))
                    continue;

                _worldPointerService.SetContent(Ids[i], _targets[i], new WorldPointerSampleVO {Text = _targets[i].name, Colour = Colours[i]});
                count++;
            }

            _signals.PointerCountChanged.Dispatch(count);
        }
    }
}
#endif