#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Constants;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Controllers
{
    /// <summary>
    /// Sends every cube a new word and tint. With the sample screen closed the service keeps the
    /// last one, and the screen shows it when it opens again.
    /// </summary>
    internal class ChangeTestContentCommand : Command
    {
        private static readonly string[] Ids = {WorldPointerSampleIds.Hide, WorldPointerSampleIds.Clamp, WorldPointerSampleIds.Ignore};
        private static readonly string[] Words = {"Hello", "Over here", "Wave!", "Ouch", "Nice", "Hmm"};

        [Inject] private IWorldPointerService _worldPointerService { get; set; }

        [SignalParam] private Transform[] _targets { get; set; }

        public override void Execute()
        {
            for (int i = 0; i < _targets.Length && i < Ids.Length; i++)
            {
                var content = new WorldPointerSampleVO
                {
                    Text = Words[Random.Range(0, Words.Length)],
                    Colour = Color.HSVToRGB(Random.value, 0.6f, 0.9f)
                };

                _worldPointerService.SetContent(Ids[i], _targets[i], content);
            }
        }
    }
}
#endif
