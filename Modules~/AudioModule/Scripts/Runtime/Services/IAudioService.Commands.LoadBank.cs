using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Loads a module's bank and holds the sequence until it is ready, so the step after it
            /// can play from it: <c>.ToSequence&lt;IAudioService.Commands.LoadBank&gt;(FlowModule.GameplayModule)</c>.
            /// A bank already loaded goes on at once. A module with no bank is reported, and the
            /// sequence goes on without its sounds rather than stopping the game.
            /// </summary>
            public class LoadBank : Command<string>
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(string module)
                {
                    Retain();
                    _audio.LoadBank(module, _ => Release());
                }
            }
        }
    }
}
