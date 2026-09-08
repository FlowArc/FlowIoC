#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Keeping one log while the rest scroll away. A reader who has found the line that matters
    /// pins it, and from then on it survives the trim that keeps the list bounded and can be shown
    /// on its own. The mark lives on the log, so it also survives a domain reload.
    /// </summary>
    public class FlowConsolePins
    {
        private readonly ConsoleLogTrimmer _trimmer = new();

        public void Toggle(ConsoleLog log)
        {
            if (log == null) return;

            log.Pinned = !log.Pinned;
        }

        /// <summary>
        /// Drops the oldest logs until the list is back inside its limit, passing over anything
        /// pinned. The rule itself is ConsoleLogTrimmer's, because FlowLogger's list is bound by
        /// the same limit and the two have to agree about what a pin means.
        /// </summary>
        public void Trim(List<ConsoleLog> logs, int maxLogCount)
        {
            _trimmer.Trim(logs, maxLogCount);
        }
    }
}
#endif