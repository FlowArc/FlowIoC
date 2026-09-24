using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>Fades the music out.</summary>
            public class StopMusic : Command
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute() => _audio.StopMusic();
            }
        }
    }
}
