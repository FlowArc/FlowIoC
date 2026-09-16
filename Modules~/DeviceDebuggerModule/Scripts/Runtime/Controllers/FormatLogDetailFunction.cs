using System.Text;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// One row as text for the clipboard, everything the detail shows: the time, the kind, the
    /// channel tag, the source, the whole message with its rich text stripped, and the stack
    /// trace when the row carries a real one.
    /// </summary>
    internal class FormatLogDetailFunction : FunctionReturn<string, ConsoleLog>
    {
        private readonly LogRowTextRule _rowText = new();

        public override string Execute(ConsoleLog row)
        {
            if (row == null) return "";

            LogRowTextVO text = _rowText.Split(row);
            var result = new StringBuilder();

            result.Append(row.Hour.ToString("00")).Append(':').Append(row.Minute.ToString("00")).Append(':')
                .Append(row.Second.ToString("00")).Append('.').Append(row.Millisecond.ToString("000"))
                .Append(' ').Append(row.LogType).Append(' ').Append(text.Tag).Append(' ').Append(text.Source).Append('\n');
            result.Append(text.PlainMessage).Append('\n');

            if (!string.IsNullOrEmpty(row.StackTrace) && !row.StackTrace.StartsWith("Source not captured", System.StringComparison.Ordinal))
                result.Append('\n').Append(row.StackTrace.TrimEnd()).Append('\n');

            return result.ToString();
        }
    }
}
