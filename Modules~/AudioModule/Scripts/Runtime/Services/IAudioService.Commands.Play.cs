using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Shared;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Plays a sound effect, named where the step is bound:
            /// <c>.ToSequence&lt;IAudioService.Commands.Play&gt;(AudioKey.Gameplay.Jump)</c>.
            /// The sequence goes on at once; it does not wait for the sound to end.
            /// </summary>
            public class Play : Command<AudioKey>
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(AudioKey key) => _audio.Play(key);
            }
        }
    }
}
