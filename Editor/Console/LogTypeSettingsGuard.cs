#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Decides whether a settings object is solid enough to delete generated source on its
    /// word. A settings asset that came from disk always carries its mandatory channels; one
    /// that carries none of them was constructed empty because the asset could not be loaded,
    /// and acting on it turns a transient import failure into deleted, tracked files.
    /// </summary>
    internal class LogTypeSettingsGuard
    {
        internal bool IsTrustworthy(IEnumerable<CD_FlowConsole.FlowConsoleLogTypeCVO> logTypes)
        {
            return logTypes != null && logTypes.Any(logType => logType != null && logType.IsMandatory);
        }
    }
}

#endif
