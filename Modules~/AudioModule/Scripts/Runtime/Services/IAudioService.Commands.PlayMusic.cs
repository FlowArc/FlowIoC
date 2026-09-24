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
            /// Crossfades the music to a track, named where the step is bound. The track already
            /// playing is left alone, so a screen may ask for its music every time it opens.
            /// </summary>
            public class PlayMusic : Command<AudioKey>
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(AudioKey key) => _audio.PlayMusic(key);
            }
        }
    }
}
