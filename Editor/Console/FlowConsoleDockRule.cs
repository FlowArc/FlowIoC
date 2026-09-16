#if UNITY_EDITOR
using FlowIoC.Editor.Help.WhatsNew;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Whether the moment the Help window opens on its own is also the moment the Flow Console
    /// goes beside Unity's Console. Nothing of the Editor in it, so the cases read here.
    ///
    /// Both readings the startup makes - the introduction for a project meeting FlowIoC, What's
    /// New for one that has just updated - are a project being handed something, and the tab goes
    /// with either, once: the update is how the tab reaches every project that was on FlowIoC
    /// before there was a tab to hand out, and the record is what keeps a closed tab closed.
    /// </summary>
    internal class FlowConsoleDockRule
    {
        internal bool For(WhatsNewDecision decision, bool dockedBefore)
        {
            return decision != WhatsNewDecision.Stop && !dockedBefore;
        }
    }
}

#endif
