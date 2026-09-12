using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Which rows a channel's switch is allowed to hide - and the group mute and the isolation with
    /// it, which are the switches thrown many at a time. A switch hides a channel's chatter, and
    /// only a plain log is chatter.
    ///
    /// A warning and an error are the framework, or the game, saying something is wrong, and the
    /// Context channel is off by default while "Shared asset is not filed on any Root!" is written
    /// on it: with the switch deciding, a developer who never touched a switch never saw that
    /// report in the Flow Console while Unity's console showed it. So a report shows whatever its
    /// channel says, and the Warning and Error toggles on the bar are what hide one. A pinned row
    /// is the reader saying this one is not noise either.
    ///
    /// Both doors ask here: the window, before it applies the switches to its list, and FlowLogger,
    /// before it forwards a line to Unity's console - so the two consoles agree on what a switch
    /// hides.
    /// </summary>
    public class FlowConsoleChannelRule
    {
        public bool AnswersToChannels(ConsoleLog log)
        {
            if (log == null) return true;

            return !log.Pinned && AnswersToChannels(log.LogType);
        }

        public bool AnswersToChannels(LogType logType)
        {
            return logType == LogType.Log;
        }
    }
}
