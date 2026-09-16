using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Entities;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Rows as text, one per line - time, kind, message - with an error's stack trace under it.
    /// What the clipboard gets: the rich-text tags the logger paints the channel with are
    /// stripped, because a text editor shows them as noise.
    /// </summary>
    internal class FormatLogRowsFunction : FunctionReturn<string, IReadOnlyList<ConsoleLog>>
    {
        private readonly Regex _richText = new("<[^>]+>", RegexOptions.Compiled);

        public override string Execute(IReadOnlyList<ConsoleLog> rows)
        {
            var text = new StringBuilder();

            if (rows == null) return "";

            foreach (ConsoleLog row in rows)
            {
                if (row == null) continue;

                text.Append(row.Hour.ToString("00")).Append(':').Append(row.Minute.ToString("00")).Append(':')
                    .Append(row.Second.ToString("00")).Append('.').Append(row.Millisecond.ToString("000"))
                    .Append(' ').Append(row.LogType).Append(' ').Append(_richText.Replace(row.Message ?? "", "")).Append('\n');

                if (LogRing.IsError(row) && !string.IsNullOrEmpty(row.StackTrace))
                    text.Append(row.StackTrace.TrimEnd()).Append('\n');
            }

            return text.ToString();
        }
    }
}