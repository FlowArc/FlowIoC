using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// An option row acted on. A step runs through its trigger; a button fires its signal with
    /// the attribute's constant; a toggle, number, text or choice fires with the control's value
    /// parsed to the payload type. The panel never decides what follows - the Command bound to
    /// the signal does.
    /// </summary>
    internal class ActivateOptionCommand : Command
    {
        [Inject] private IFunctionProvider _functionProvider { get; set; }

        [SignalParam] private DebugOptionVO _option { get; set; }
        [SignalParam] private string _text { get; set; }

        public override void Execute()
        {
            if (_option == null) return;

            switch (_option.Kind)
            {
                case DebugOptionKind.Button:
                    Fire();
                    break;
                case DebugOptionKind.Toggle:
                case DebugOptionKind.Number:
                case DebugOptionKind.Text:
                case DebugOptionKind.Choice:
                    FireWithValue();
                    break;
                default:
                    FlowLogger.Log("Option " + _option.Label + " is " + _option.Kind + " and takes no action.");
                    break;
            }
        }

        private void Fire()
        {
            if (_option.Trigger != null)
            {
                _option.Trigger.Dispatch();
                return;
            }

            if (_option.Signal == null) return;

            if (_option.Argument != null) _option.Signal.DispatchUntyped(_option.Argument);
            else _option.Signal.DispatchUntyped();
        }

        private void FireWithValue()
        {
            if (_option.Signal == null || _option.PayloadType == null) return;

            ParsedPayloadVO parsed = _functionProvider.Call<ParseSignalPayloadFunction>()
                .AddParams(_option.PayloadType, _text ?? "")
                .ExecuteAndGetResult<ParsedPayloadVO>();

            if (!parsed.Ok)
            {
                FlowLogger.LogWarning("Option " + _option.Label + ": " + parsed.Reason + ".");
                return;
            }

            _option.Signal.DispatchUntyped(parsed.Value);
        }
    }
}
