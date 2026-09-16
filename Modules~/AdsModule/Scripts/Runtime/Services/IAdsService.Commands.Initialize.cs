using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Initializes the plugged SDK. Only needed when CD_Ads.InitializeOnLaunch is off:
            /// bound behind the game's consent step, <c>.ToSequence&lt;IAdsService.Commands.SetConsent&gt;()
            /// .ToSequence&lt;IAdsService.Commands.Initialize&gt;()</c>.
            /// </summary>
            [DebugOption("Ads", "Initialize")]
            public class Initialize : Command
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute() => _ads.Initialize();
            }
        }
    }
}
