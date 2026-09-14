using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.ScreenModule.Service
{
    public partial interface IScreenService
    {
        public static partial class Commands
        {
            /// <summary>Unloads every loaded screen. A screen on stage is hidden first.</summary>
            public class UnloadAll : Command
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute() => _screenService.Unload.AllScreens();
            }
        }
    }
}
