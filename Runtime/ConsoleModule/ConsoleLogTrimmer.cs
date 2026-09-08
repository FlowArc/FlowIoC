using System.Collections.Generic;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Keeps a log list inside its limit by dropping the oldest, and passes over anything the
    /// reader pinned. It lives here rather than in the console window because two lists are bound
    /// by the same limit - FlowLogger's, and the window's own copy - and they must agree about
    /// what a pin means.
    /// </summary>
    public class ConsoleLogTrimmer
    {
        /// <summary>
        /// A reader who pins more than the limit keeps them all: the list going over is better
        /// than throwing away what they said to keep.
        /// </summary>
        public void Trim(List<ConsoleLog> logs, int maxLogCount)
        {
            if (logs == null) return;
            if (maxLogCount <= 0) return;

            int over = logs.Count - maxLogCount;
            if (over <= 0) return;

            int at = 0;

            while (over > 0 && at < logs.Count)
            {
                if (logs[at].Pinned)
                {
                    at++;
                    continue;
                }

                logs.RemoveAt(at);
                over--;
            }
        }
    }
}
