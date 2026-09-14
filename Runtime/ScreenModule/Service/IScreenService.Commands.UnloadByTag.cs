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
            /// Unloads every loaded screen of the tag, the tag given where the step is bound:
            /// <c>.ToSequence&lt;IScreenService.Commands.UnloadByTag&gt;(ScreenTag.GroupB)</c> - the
            /// match's screens going out of memory when the match is left. A screen on stage is
            /// hidden first.
            /// </summary>
            public class UnloadByTag : Command<ScreenTag>
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute(ScreenTag tag) => _screenService.Unload.ScreensByTag(tag);
            }
        }
    }
}
