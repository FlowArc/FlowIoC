using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>Stops a module's sounds and hands its clips back: <c>.ToSequence&lt;IAudioService.Commands.UnloadBank&gt;(FlowModule.GameplayModule)</c>.</summary>
            public class UnloadBank : Command<string>
            {
                [Inject] private IAudioService _audio { get; set; }

                public override void Execute(string module) => _audio.UnloadBank(module);
            }
        }
    }
}
