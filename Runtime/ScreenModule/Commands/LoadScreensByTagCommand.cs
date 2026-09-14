using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Service;

namespace FlowIoC.ScreenModule.Commands
{
    /// <summary>
    /// A step a game binds in a sequence of its own, the tag given where the step is bound:
    /// <c>.ToSequence&lt;LoadScreensByTagCommand&gt;(ScreenTag.GroupA)</c>. It loads every screen
    /// registered under the tag into the pool and holds the sequence until the last of them is in,
    /// so the step after it can open one of those screens and have it on stage the same frame - a
    /// boot loads its loading screens this way before the loading set begins, and then the set's
    /// own work is what the bar shows. A screen already loaded is passed over.
    ///
    /// The load reports failure where it happens - the missing Root, the bad key - and the
    /// callback never comes; a sequence that has to notice that waits on a loading set with a
    /// stall watcher rather than on this step alone.
    /// </summary>
    public class LoadScreensByTagCommand : Command<ScreenTag>
    {
        [Inject] private IScreenService _screenService { get; set; }

        public override void Execute(ScreenTag tag)
        {
            Retain();
            _screenService.Load.ByTag(tag, completeCallback: () => Release());
        }
    }
}
