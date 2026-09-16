using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>The plugged SDK's own debug screen - MAX's mediation debugger - for the Device Debugger's Ads rows.</summary>
            [DebugOption("Ads", "Mediation debugger")]
            public class ShowProviderDebugger : Command
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute() => _ads.ShowProviderDebugger();
            }
        }
    }
}
