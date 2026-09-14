using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Service
{
    public partial interface IScreenService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Hides every screen of the tag that is on stage, the tag given where the step is
            /// bound: <c>.ToSequence&lt;IScreenService.Commands.HideByTag&gt;(ScreenTag.GroupB)</c>.
            /// Each screen plays its hide animation on its own; the step does not wait for it.
            /// </summary>
            public class HideByTag : Command<ScreenTag>
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute(ScreenTag tag) => _screenService.Hide.ScreensByTag(tag);
            }
        }
    }
}
