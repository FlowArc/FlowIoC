using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Shared;
using UnityEngine;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Plays a sound effect from a point in the world. The key is named where the step is
            /// bound; the point comes off the signal the sequence is bound to, which carries a
            /// Vector3: <c>CommandBinder.Bind(_signals.Incoming.Exploded).ToSequence&lt;IAudioService.Commands.PlayAt&gt;(AudioKey.Gameplay.Boom)</c>.
            /// </summary>
            public class PlayAt : Command<AudioKey>
            {
                [SignalParam] private Vector3 _position { get; set; }

                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(AudioKey key) => _audio.PlayAt(key, _position);
            }
        }
    }
}
