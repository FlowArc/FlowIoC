using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service
{
    public interface IScreenService
    {
        LoadSubService Load { get; }

        /// <summary><i><b>&gt;FlowIoC&lt;</b></i>
        /// <br></br>You have to use the "Show" method at the end of the line
        /// <br></br><b>Example:</b>
        /// <br></br>_screenService.Open&lt;NewScreenViewn&gt;().<b>Show()</b>;
        /// <br></br>var scrn = await _service.Open&lt;New...<b>Show&lt;NewScreenView&gt;()</b>;
        /// </summary>
        /// <typeparam name="T">IScreenBody (Example:NewScreenView)</typeparam>
        /// <returns></returns>
        IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody;

        CheckSubService Check { get; }
        TryGetSubService TryGet { get; }
        HideSubService Hide { get; }
        UnloadSubService Unload { get; }

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found:
        /// <c>.ToSequence&lt;IScreenService.Commands.LoadByTag&gt;(ScreenTag.GroupA)</c>.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Loads every screen registered under the tag into the pool and holds the sequence
            /// until the last of them is in, so the step after it can open one of those screens
            /// and have it on stage the same frame - a boot loads its loading screens this way
            /// before the loading set begins, and then the set's own work is what the bar shows. A
            /// screen already loaded is passed over.
            ///
            /// The load reports failure where it happens - the missing Root, the bad key - and the
            /// callback never comes; a sequence that has to notice that waits on a loading set
            /// with a stall watcher rather than on this step alone.
            /// </summary>
            public class LoadByTag : Command<ScreenTag>
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute(ScreenTag tag)
                {
                    Retain();
                    _screenService.Load.ByTag(tag, completeCallback: () => Release());
                }
            }
        }
    }
}
