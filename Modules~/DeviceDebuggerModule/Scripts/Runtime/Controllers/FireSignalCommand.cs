using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>A Signals-tab row fired: no payload, or the one payload parsed from the text beside it.</summary>
    internal class FireSignalCommand : Command
    {
        [Inject] private IFunctionProvider _functionProvider { get; set; }

        [SignalParam] private DebugSignalVO _row { get; set; }
        [SignalParam] private string _text { get; set; }

        public override void Execute()
        {
            if (_row?.Signal == null || !_row.CanFire) return;

            if (_row.PayloadTypes.Length == 0)
            {
                _row.Signal.DispatchUntyped();
                return;
            }

            ParsedPayloadVO parsed = _functionProvider.Call<ParseSignalPayloadFunction>()
                .AddParams(_row.PayloadTypes[0], _text ?? "")
                .ExecuteAndGetResult<ParsedPayloadVO>();

            if (!parsed.Ok)
            {
                FlowLogger.LogWarning("Signal " + _row.SignalName + ": " + parsed.Reason + ".");
                return;
            }

            _row.Signal.DispatchUntyped(parsed.Value);
        }
    }
}
