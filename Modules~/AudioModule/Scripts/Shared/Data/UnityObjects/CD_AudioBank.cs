using System.Collections.Generic;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// A module's sounds. It lives in that module, at <c>Resources/Audio/CD_AudioBank.asset</c>,
    /// never in the Audio module: the Audio service finds every bank in the project by itself,
    /// and an update of Audio has nothing of the game's to overwrite. The clips are Addressables
    /// references, so a bank found at boot holds no clip in memory until it is loaded.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AudioBank", menuName = "FlowIoC/AudioModule/Data/CD_AudioBank")]
    public class CD_AudioBank : ScriptableObject
    {
        [Tooltip("The module whose sounds these are, as FlowModule names it. Every key in this bank starts with it, and IAudioService.Commands.LoadBank takes it.")]
        public string Module;

        [Tooltip("Load the clips when the game starts. Off, the module binds IAudioService.Commands.LoadBank where it needs them.")]
        public bool PreloadAtBoot = true;

        public List<AudioClipCVO> Sounds = new();
    }
}
