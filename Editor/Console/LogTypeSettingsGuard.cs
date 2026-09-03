#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Decides whether a settings object is solid enough to delete generated source on its word.
    ///
    /// The object has to say whether it came from disk, because counting channels cannot tell.
    /// The stand-in <see cref="FlowLogger"/> builds when the asset will not load is filled by
    /// ResetToDefaults, which adds exactly the mandatory channels - so a stand-in looks like a
    /// freshly authored asset, and acting on it turns a transient import failure into deleted,
    /// tracked files. A settings asset that came from disk always carries its mandatory channels
    /// too, so one that carries none of them is not to be trusted either.
    /// </summary>
    internal class LogTypeSettingsGuard
    {
        internal bool IsTrustworthy(bool isStandIn, IEnumerable<CD_FlowConsole.FlowConsoleLogTypeCVO> logTypes)
        {
            if (isStandIn) return false;

            return logTypes != null && logTypes.Any(logType => logType != null && logType.IsMandatory);
        }
    }
}

#endif
