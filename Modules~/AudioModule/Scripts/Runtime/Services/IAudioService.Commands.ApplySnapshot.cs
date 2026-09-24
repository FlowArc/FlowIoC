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
            /// Moves the mixer to a snapshot, named where the step is bound:
            /// <c>.ToSequence&lt;IAudioService.Commands.ApplySnapshot&gt;(AudioSnapshot.Ducked)</c>.
            /// </summary>
            public class ApplySnapshot : Command<AudioSnapshot>
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(AudioSnapshot snapshot) => _audio.ApplySnapshot(snapshot);
            }
        }
    }
}
