using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.ScreenModule.Service
{
    public partial interface IScreenService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Hides every screen on stage. Each screen plays its hide animation on its own; the
            /// step does not wait for it.
            /// </summary>
            public class HideAll : Command
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute() => _screenService.Hide.AllScreens();
            }
        }
    }
}
