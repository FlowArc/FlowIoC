#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.Shared.Constants;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>Asks every cube's pointer to hide, or to show again.</summary>
    internal class SetTestPointersHiddenCommand : Command
    {
        private static readonly string[] Ids = {WorldPointerSampleIds.Hide, WorldPointerSampleIds.Clamp, WorldPointerSampleIds.Ignore};

        [Inject] private IWorldPointerService _worldPointerService { get; set; }

        [SignalParam] private Transform[] _targets { get; set; }
        [SignalParam] private bool _hidden { get; set; }

        public override void Execute()
        {
            for (int i = 0; i < _targets.Length && i < Ids.Length; i++)
            {
                if (_hidden)
                    _worldPointerService.Hide(Ids[i], _targets[i]);
                else
                    _worldPointerService.Show(Ids[i], _targets[i]);
            }
        }
    }
}
#endif
